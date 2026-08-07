using Windows.Media.Control;

namespace ZoyaRadio.MediaProbe;

internal enum AiCommentRunMode
{
    Spoken,
    GenerateOnly,
    Measure,
}

internal sealed class AiCommentTest
{
    private readonly GlobalSystemMediaTransportControlsSessionManager _manager;
    private readonly LocalLog _log;
    private readonly WindowsSpeechService? _speechService;
    private readonly ILocalCommentGenerator _generator;
    private readonly AiCommentConfiguration _configuration;

    public AiCommentTest(
        GlobalSystemMediaTransportControlsSessionManager manager,
        LocalLog log,
        WindowsSpeechService? speechService,
        ILocalCommentGenerator generator,
        AiCommentConfiguration configuration)
    {
        _manager = manager;
        _log = log;
        _speechService = speechService;
        _generator = generator;
        _configuration = configuration;
    }

    public async Task<int> RunAsync(
        AiCommentRunMode mode,
        bool aiEnabled,
        bool simulateAiFailure,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            GlobalSystemMediaTransportControlsSession[] sessions;

            try
            {
                sessions = _manager.GetSessions().ToArray();
            }
            catch (Exception exception)
            {
                _log.Warning("Could not enumerate Windows media sessions for the AI comment test", exception);
                _log.Write("AI COMMENT TEST RESULT: FAIL - no playback or speech commands were issued.");
                return 1;
            }

            _log.Write($"AVAILABLE WINDOWS MEDIA SESSIONS: {sessions.Length}");
            var details = new List<AiTargetDetails>(sessions.Length);

            for (int index = 0; index < sessions.Length; index++)
            {
                AiTargetDetails current = await ReadDetailsAsync(sessions[index]);
                details.Add(current);
                WriteDetails($"SESSION {index + 1}", current);
            }

            IReadOnlyList<AiTargetDetails> matches = FirefoxSessionSelector.FindExactMatches(
                details,
                candidate => candidate.SourceId);

            if (matches.Count == 0)
            {
                _log.Write("NO FIREFOX TARGET FOUND: no AI, playback, or speech commands were issued.");
                _log.Write("Start YouTube Music in Firefox, then run this test again.");
                _log.Write("AI COMMENT TEST RESULT: NOT RUN");
                return 1;
            }

            if (matches.Count > 1)
            {
                _log.Write("!!! AMBIGUOUS TARGET !!!");
                _log.Write($"Found {matches.Count} sessions matching firefox.exe. Nothing was controlled.");

                for (int index = 0; index < matches.Count; index++)
                {
                    WriteDetails($"FIREFOX CANDIDATE {index + 1}", matches[index]);
                }

                _log.Write("Close the extra Firefox media session(s) and try again.");
                _log.Write("AI COMMENT TEST RESULT: NOT RUN");
                return 1;
            }

            GlobalSystemMediaTransportControlsSession selectedSession = matches[0].Session;
            AiTargetDetails target = await ReadDetailsAsync(selectedSession);
            WriteDetails("TARGET", target);

            if (!target.MediaReadSucceeded || !AnnouncementText.TryBuild(target.Media, out _))
            {
                _log.Write("METADATA SAFETY: Firefox did not provide both a usable title and artist.");
                _log.Write("No AI, playback, or speech commands were issued. Try again while a song is available.");
                _log.Write("AI COMMENT TEST RESULT: NOT RUN");
                return 1;
            }

            if (mode == AiCommentRunMode.Measure)
            {
                var diagnostic = new AiPerformanceDiagnostic(_generator, _configuration, _log);
                AiPerformanceResult result = await diagnostic.RunAsync(target.Media, cancellationToken);
                return result.GenerationSucceeded && result.ModelObservedLoaded && result.ModelUnloadVerified ? 0 : 1;
            }

            if (mode == AiCommentRunMode.Spoken
                && (!target.PlaybackReadSucceeded || !PlaybackControlRules.CanStart(target.PlaybackStatus)))
            {
                _log.Write($"STARTING-STATE SAFETY: Firefox is {DisplayStatus(target.PlaybackStatus)}, not Playing.");
                _log.Write("No AI, playback, or speech commands were issued. Start YouTube Music playing and try again.");
                _log.Write("AI COMMENT TEST RESULT: NOT RUN");
                return 1;
            }

            var commentService = new AiCommentService(_generator, _configuration, _log);
            CommentSelection selection = await commentService.SelectCommentAsync(
                target.Media,
                aiEnabled,
                simulateAiFailure,
                cancellationToken);

            _log.Write($"Title: {MediaSnapshot.Display(target.Media.Title)}");
            _log.Write($"Artist: {MediaSnapshot.Display(target.Media.Artist)}");
            _log.Write($"Model: {(aiEnabled ? _generator.ModelName : "<AI disabled>")}");
            _log.Write($"Final text source: {(selection.Source == CommentTextSource.Ai ? "AI" : "FIXED FALLBACK")}");
            _log.Write($"Final text: {selection.Text}");

            if (mode == AiCommentRunMode.GenerateOnly)
            {
                _log.Write("GENERATE-ONLY: no playback or speech commands were issued.");
                bool safeGeneration = IsSafeGenerateOnlyResult(selection);
                string result = selection.ModelRequestedFallback
                    ? "PASS - SAFE FALLBACK USED"
                    : safeGeneration
                        ? "PASS"
                        : "AI FAILURE FALLBACK USED";
                _log.Write($"GENERATE-ONLY RESULT: {result}");
                return safeGeneration ? 0 : 1;
            }

            if (_speechService is null)
            {
                _log.Write("AI COMMENT TEST RESULT: NOT RUN - no usable local Windows voice was available.");
                return 1;
            }

            _log.Write("Speak this comment and temporarily pause Firefox? [y/N]");
            string? response = await Console.In.ReadLineAsync(cancellationToken);

            if (!PlaybackControlRules.IsAffirmative(response))
            {
                _log.Write("User confirmation: NO");
                _log.Write("AI COMMENT TEST CANCELLED: no playback or speech commands were issued.");
                _log.Write("AI COMMENT TEST RESULT: NOT RUN");
                return 0;
            }

            _log.Write("User confirmation: YES");
            cancellationToken.ThrowIfCancellationRequested();

            AiTargetDetails rechecked = await ReadDetailsAsync(selectedSession);
            if (!AiCommentSafety.CanSpeakPreparedComment(
                    target.Media,
                    rechecked.SourceId,
                    rechecked.SourceReadSucceeded,
                    rechecked.Media,
                    rechecked.MediaReadSucceeded,
                    rechecked.PlaybackStatus,
                    rechecked.PlaybackReadSucceeded))
            {
                _log.Write("STALE-METADATA SAFETY: Firefox state or title/artist changed while text was prepared.");
                _log.Write("No playback or speech commands were issued. Check the current song and run the test again.");
                _log.Write("AI COMMENT TEST RESULT: NOT RUN");
                return 1;
            }

            IPreparedAnnouncementSpeech preparedSpeech;

            try
            {
                _log.Write("Preparing the confirmed Unicode comment with the selected local voice...");
                preparedSpeech = await _speechService.PrepareAsync(selection.Text);
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                _log.Write("AI COMMENT TEST CANCELLED before pause: no playback commands were issued.");
                _log.Write("AI COMMENT TEST RESULT: NOT RUN");
                return 0;
            }
            catch (Exception exception)
            {
                _log.Warning("Windows could not prepare the AI comment for speech", exception);
                _log.Write("SPEECH FAILED before playback was paused.");
                _log.Write("AI COMMENT TEST RESULT: FAIL");
                return 1;
            }

            using (preparedSpeech)
            {
                var targetControl = new WindowsAnnouncementPlaybackTarget(selectedSession, _log);
                var sequence = new AnnouncementControlSequence(_log);
                AnnouncementSequenceResult result = await sequence.RunAsync(
                    targetControl,
                    preparedSpeech,
                    simulateSpeechFailure: false,
                    cancellationToken);
                _log.Write($"AI COMMENT TEST RESULT: {(result.TestPassed ? "PASS" : "FAIL")}");
                return result.TestPassed ? 0 : 1;
            }
        }
        catch (OperationCanceledException)
        {
            _log.Write("AI COMMENT TEST CANCELLED before pause: no playback commands were issued.");
            _log.Write("AI COMMENT TEST RESULT: NOT RUN");
            return 0;
        }
        catch (Exception exception)
        {
            _log.Warning("Unexpected failure before the AI comment control sequence", exception);
            _log.Write("AI COMMENT TEST RESULT: FAIL - no playback or speech commands were issued.");
            return 1;
        }
    }

    internal static bool IsSafeGenerateOnlyResult(CommentSelection selection)
    {
        return selection.Source == CommentTextSource.Ai || selection.ModelRequestedFallback;
    }

    private async Task<AiTargetDetails> ReadDetailsAsync(
        GlobalSystemMediaTransportControlsSession session)
    {
        string? sourceId = null;
        MediaSnapshot media = MediaSnapshot.Empty;
        bool sourceReadSucceeded = false;
        bool mediaReadSucceeded = false;

        try
        {
            sourceId = session.SourceAppUserModelId;
            sourceReadSucceeded = true;
        }
        catch (Exception exception)
        {
            _log.Warning("Could not read one session's source application identifier", exception);
        }

        try
        {
            GlobalSystemMediaTransportControlsSessionMediaProperties? properties =
                await session.TryGetMediaPropertiesAsync();

            if (properties is not null)
            {
                media = MediaSnapshot.FromWindows(properties.Title, properties.Artist, properties.AlbumTitle);
                mediaReadSucceeded = true;
            }
        }
        catch (Exception exception)
        {
            _log.Warning($"Could not read media properties for {MediaSnapshot.Display(sourceId)}", exception);
        }

        PlaybackStatusRead playback = TryReadPlaybackStatus(session);
        return new AiTargetDetails(
            session,
            sourceId,
            sourceReadSucceeded,
            media,
            mediaReadSucceeded,
            playback.Status,
            playback.Success);
    }

    private PlaybackStatusRead TryReadPlaybackStatus(
        GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            GlobalSystemMediaTransportControlsSessionPlaybackInfo? info = session.GetPlaybackInfo();
            return new PlaybackStatusRead(info is not null, info?.PlaybackStatus);
        }
        catch (Exception exception)
        {
            _log.Warning("Could not read playback state while reading session details", exception);
            return new PlaybackStatusRead(false, null);
        }
    }

    private void WriteDetails(string heading, AiTargetDetails details)
    {
        _log.Write(heading);
        _log.Write($"  SourceAppUserModelId: {MediaSnapshot.Display(details.SourceId)}");
        _log.Write($"  Title: {MediaSnapshot.Display(details.Media.Title)}");
        _log.Write($"  Artist: {MediaSnapshot.Display(details.Media.Artist)}");
        _log.Write($"  Album: {MediaSnapshot.Display(details.Media.Album)}");
        _log.Write($"  Playback: {DisplayStatus(details.PlaybackStatus)}");
    }

    private static string DisplayStatus(
        GlobalSystemMediaTransportControlsSessionPlaybackStatus? status)
    {
        return status?.ToString() ?? "<not provided>";
    }

    private sealed record AiTargetDetails(
        GlobalSystemMediaTransportControlsSession Session,
        string? SourceId,
        bool SourceReadSucceeded,
        MediaSnapshot Media,
        bool MediaReadSucceeded,
        GlobalSystemMediaTransportControlsSessionPlaybackStatus? PlaybackStatus,
        bool PlaybackReadSucceeded);
}
