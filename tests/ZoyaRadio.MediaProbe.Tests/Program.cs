using System.Net;
using System.Text.Json;
using Windows.Media.Control;
using ZoyaRadio.MediaProbe;

var tests = new (string Name, Func<Task> Run)[]
{
    ("zero Firefox matches", () => RunSynchronous(ZeroFirefoxMatches)),
    ("case-insensitive exact Firefox match", () => RunSynchronous(CaseInsensitiveExactMatch)),
    ("partial Firefox name is not a match", () => RunSynchronous(PartialNameDoesNotMatch)),
    ("multiple Firefox matches remain ambiguous", () => RunSynchronous(MultipleFirefoxMatches)),
    ("confirmation defaults to no", () => RunSynchronous(ConfirmationDefaultsToNo)),
    ("only explicit y or yes confirms", () => RunSynchronous(ExplicitConfirmationOnly)),
    ("control starts only from Playing", () => RunSynchronous(StartingStateMustBePlaying)),
    ("announcement requires title and artist", () => RunSynchronous(AnnouncementRequiresMetadata)),
    ("announcement preserves Unicode text", () => RunSynchronous(AnnouncementPreservesUnicode)),
    ("metadata recheck compares title and artist", () => RunSynchronous(MetadataRecheckUsesTitleAndArtist)),
    ("normal announcement sequence restores playback", NormalSequenceRestoresPlayback),
    ("speech failure uses protected playback recovery", SpeechFailureRestoresPlayback),
    ("speech timeout uses protected playback recovery", SpeechTimeoutRestoresPlayback),
    ("simulated speech failure uses real recovery path", SimulatedFailureRestoresPlayback),
    ("speech cancellation uses protected playback recovery", SpeechCancellationRestoresPlayback),
    ("Ollama endpoint permits loopback only", () => RunSynchronous(OllamaEndpointIsLoopbackOnly)),
    ("host persona defaults to Zoya and Steve", () => RunSynchronous(HostPersonaUsesExpectedDefaults)),
    ("host and listener names render from configuration", () => RunSynchronous(HostPersonaRendersConfiguredNames)),
    ("comment prompt sends only title and artist metadata", () => RunSynchronous(CommentPromptUsesAllowedMetadataOnly)),
    ("comment prompt covers manual grounding failures", () => RunSynchronous(CommentPromptCoversManualGroundingFailures)),
    ("persona prompt distinguishes safe imagination from unsupported facts", () => RunSynchronous(PersonaPromptDocumentsCreativeBoundary)),
    ("persona configuration reaches comment generation", PersonaConfigurationReachesGenerator),
    ("Ollama request is local, non-streaming, non-thinking, and unload-on-return", OllamaRequestUsesSafeOptions),
    ("comment validator normalizes harmless whitespace", () => RunSynchronous(CommentValidatorNormalizesWhitespace)),
    ("comment validator rejects empty output", () => RunSynchronous(CommentValidatorRejectsEmpty)),
    ("comment validator rejects short output", () => RunSynchronous(CommentValidatorRejectsShort)),
    ("comment validator rejects long output", () => RunSynchronous(CommentValidatorRejectsLong)),
    ("comment validator rejects multiple paragraphs", () => RunSynchronous(CommentValidatorRejectsParagraphs)),
    ("comment validator rejects commercial radio cliches", () => RunSynchronous(CommentValidatorRejectsCliches)),
    ("comment validator rejects Milestone 4 radio stock phrases", () => RunSynchronous(CommentValidatorRejectsPersonaCliches)),
    ("comment validator rejects model commentary", () => RunSynchronous(CommentValidatorRejectsModelCommentary)),
    ("exact FALLBACK selects fixed template", ExactFallbackSelectsFixedTemplate),
    ("FALLBACK sentinel is never final spoken text", FallbackSentinelIsNeverFinalText),
    ("safe FALLBACK is a successful generate-only result", SafeFallbackIsSuccessfulGenerateOnlyResult),
    ("HTTP failure selects fixed fallback", HttpFailureUsesFallback),
    ("timeout selects fixed fallback", TimeoutUsesFallback),
    ("malformed response selects fixed fallback", MalformedResponseUsesFallback),
    ("unexpected generator exception still unloads and falls back", GeneratorExceptionStillUnloads),
    ("valid AI comment still unloads selected model", ValidAiCommentStillUnloads),
    ("no-AI mode bypasses Ollama", NoAiBypassesOllama),
    ("simulated AI failure bypasses Ollama", SimulatedAiFailureBypassesOllama),
    ("stale track blocks prepared comment", () => RunSynchronous(StaleTrackBlocksPreparedComment)),
};

int failures = 0;

foreach ((string name, Func<Task> run) in tests)
{
    try
    {
        await run();
        Console.WriteLine($"PASS: {name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.WriteLine($"FAIL: {name} - {exception.Message}");
    }
}

Console.WriteLine($"RESULT: {tests.Length - failures} passed, {failures} failed");
return failures == 0 ? 0 : 1;

static void ZeroFirefoxMatches()
{
    var sessions = new[] { new FakeSession("chrome.exe"), new FakeSession(null) };
    AssertEqual(0, Select(sessions).Count);
}

static void CaseInsensitiveExactMatch()
{
    var expected = new FakeSession("FIREFOX.EXE");
    IReadOnlyList<FakeSession> matches = Select(new[] { expected, new FakeSession("chrome.exe") });
    AssertEqual(1, matches.Count);
    AssertSame(expected, matches[0]);
}

static void PartialNameDoesNotMatch()
{
    AssertEqual(0, Select(new[] { new FakeSession("myfirefox.exe") }).Count);
}

static void MultipleFirefoxMatches()
{
    IReadOnlyList<FakeSession> matches = Select(new[]
    {
        new FakeSession("firefox.exe"),
        new FakeSession("FIREFOX.EXE"),
    });

    AssertEqual(2, matches.Count);
}

static void ConfirmationDefaultsToNo()
{
    AssertFalse(PlaybackControlRules.IsAffirmative(null));
    AssertFalse(PlaybackControlRules.IsAffirmative(string.Empty));
    AssertFalse(PlaybackControlRules.IsAffirmative("n"));
    AssertFalse(PlaybackControlRules.IsAffirmative("yeah"));
}

static void ExplicitConfirmationOnly()
{
    AssertTrue(PlaybackControlRules.IsAffirmative("y"));
    AssertTrue(PlaybackControlRules.IsAffirmative(" YES "));
}

static void StartingStateMustBePlaying()
{
    AssertTrue(PlaybackControlRules.CanStart(GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing));
    AssertFalse(PlaybackControlRules.CanStart(GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused));
    AssertFalse(PlaybackControlRules.CanStart(GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped));
    AssertFalse(PlaybackControlRules.CanStart(null));
}

static void AnnouncementRequiresMetadata()
{
    AssertFalse(AnnouncementText.TryBuild(MediaSnapshot.Empty, out _));
    AssertFalse(AnnouncementText.TryBuild(new MediaSnapshot("Song", null, null), out _));
    AssertFalse(AnnouncementText.TryBuild(new MediaSnapshot(null, "Artist", null), out _));
}

static void AnnouncementPreservesUnicode()
{
    var media = MediaSnapshot.FromWindows("Кукушка", "Кино", null);
    AssertTrue(AnnouncementText.TryBuild(media, out string? sentence));
    AssertEqual("That was Кукушка by Кино.", sentence!);
}

static void MetadataRecheckUsesTitleAndArtist()
{
    var expected = new MediaSnapshot("Cochise", "Audioslave", null);
    var sameTrackWithAlbum = new MediaSnapshot("Cochise", "Audioslave", "Optional album");
    var differentTrack = new MediaSnapshot("Know Your Enemy", "Rage Against The Machine", null);

    AssertTrue(AnnouncementText.HasSameTitleAndArtist(expected, sameTrackWithAlbum));
    AssertFalse(AnnouncementText.HasSameTitleAndArtist(expected, differentTrack));
}

static async Task NormalSequenceRestoresPlayback()
{
    var target = new FakePlaybackTarget();
    var speech = new FakePreparedSpeech(new SpeechPlaybackResult(SpeechPlaybackStatus.Completed));
    var log = new FakeLog();
    var sequence = new AnnouncementControlSequence(log, FastTimings());

    AnnouncementSequenceResult result = await sequence.RunAsync(
        target,
        speech,
        simulateSpeechFailure: false,
        CancellationToken.None);

    AssertTrue(result.TestPassed);
    AssertTrue(result.PauseVerified);
    AssertTrue(result.PlaybackRestored);
    AssertFalse(result.RecoveryNeeded);
    AssertEqual(1, target.PauseCalls);
    AssertEqual(1, target.PlayCalls);
    AssertEqual(1, speech.SpeakCalls);
}

static async Task SpeechFailureRestoresPlayback()
{
    var target = new FakePlaybackTarget();
    var speech = new FakePreparedSpeech(
        new SpeechPlaybackResult(SpeechPlaybackStatus.Failed, "Intentional fake failure."));
    var log = new FakeLog();
    var sequence = new AnnouncementControlSequence(log, FastTimings());

    AnnouncementSequenceResult result = await sequence.RunAsync(
        target,
        speech,
        simulateSpeechFailure: false,
        CancellationToken.None);

    AssertFalse(result.TestPassed);
    AssertTrue(result.PlaybackRestored);
    AssertTrue(result.RecoveryNeeded);
    AssertEqual(1, target.PlayCalls);
    AssertTrue(log.Contains("PLAYBACK RECOVERY: PASS"));
}

static async Task SimulatedFailureRestoresPlayback()
{
    var target = new FakePlaybackTarget();
    var log = new FakeLog();
    var sequence = new AnnouncementControlSequence(log, FastTimings());

    AnnouncementSequenceResult result = await sequence.RunAsync(
        target,
        preparedSpeech: null,
        simulateSpeechFailure: true,
        CancellationToken.None);

    AssertTrue(result.TestPassed);
    AssertTrue(result.SimulatedFailure);
    AssertTrue(result.PlaybackRestored);
    AssertTrue(result.RecoveryNeeded);
    AssertEqual(1, target.PauseCalls);
    AssertEqual(1, target.PlayCalls);
    AssertTrue(log.Contains("SIMULATED SPEECH-FAILURE TEST RESULT: PASS"));
}

static async Task SpeechTimeoutRestoresPlayback()
{
    var target = new FakePlaybackTarget();
    var speech = new FakePreparedSpeech(new SpeechPlaybackResult(SpeechPlaybackStatus.TimedOut));
    var log = new FakeLog();
    var sequence = new AnnouncementControlSequence(log, FastTimings());

    AnnouncementSequenceResult result = await sequence.RunAsync(
        target,
        speech,
        simulateSpeechFailure: false,
        CancellationToken.None);

    AssertFalse(result.TestPassed);
    AssertTrue(result.PlaybackRestored);
    AssertTrue(result.RecoveryNeeded);
    AssertTrue(log.Contains("SPEECH TIMEOUT"));
    AssertTrue(log.Contains("PLAYBACK RECOVERY: PASS"));
}

static async Task SpeechCancellationRestoresPlayback()
{
    var target = new FakePlaybackTarget();
    var speech = new FakePreparedSpeech(throwCancellation: true);
    var log = new FakeLog();
    var sequence = new AnnouncementControlSequence(log, FastTimings());

    AnnouncementSequenceResult result = await sequence.RunAsync(
        target,
        speech,
        simulateSpeechFailure: false,
        CancellationToken.None);

    AssertFalse(result.TestPassed);
    AssertTrue(result.PlaybackRestored);
    AssertTrue(result.RecoveryNeeded);
    AssertEqual(1, target.PlayCalls);
}

static void OllamaEndpointIsLoopbackOnly()
{
    AssertTrue(OllamaEndpointPolicy.IsAllowed(new Uri("http://localhost:11434/api/")));
    AssertTrue(OllamaEndpointPolicy.IsAllowed(new Uri("http://127.0.0.1:11434/api")));
    AssertTrue(OllamaEndpointPolicy.IsAllowed(new Uri("http://[::1]:11434/api/")));
    AssertFalse(OllamaEndpointPolicy.IsAllowed(new Uri("http://example.com:11434/api/")));
    AssertFalse(OllamaEndpointPolicy.IsAllowed(new Uri("https://localhost:11434/api/")));
    AssertFalse(OllamaEndpointPolicy.IsAllowed(new Uri("http://user:secret@localhost:11434/api/")));
    AssertFalse(OllamaEndpointPolicy.IsAllowed(new Uri("http://localhost:11434/not-api/")));
}

static void HostPersonaUsesExpectedDefaults()
{
    AssertEqual("Zoya", HostPersonaConfiguration.Default.HostName);
    AssertEqual("Steve", HostPersonaConfiguration.Default.ListenerName);

    CommentPrompt prompt = CommentPromptFactory.Create(TestMedia());
    AssertContains(prompt.SystemInstruction, "You are Zoya");
    AssertContains(prompt.SystemInstruction, "sitting beside Steve");
}

static void HostPersonaRendersConfiguredNames()
{
    var persona = new HostPersonaConfiguration("Test Host", "Test Listener");
    CommentPrompt prompt = CommentPromptFactory.Create(TestMedia(), persona);

    AssertContains(prompt.SystemInstruction, "You are Test Host");
    AssertContains(prompt.SystemInstruction, "sitting beside Test Listener");
    AssertFalse(prompt.SystemInstruction.Contains("{{HOST_NAME}}", StringComparison.Ordinal));
    AssertFalse(prompt.SystemInstruction.Contains("{{LISTENER_NAME}}", StringComparison.Ordinal));
}

static void CommentPromptUsesAllowedMetadataOnly()
{
    var media = new MediaSnapshot("\u041A\u0443\u043A\u0443\u0448\u043A\u0430", "\u041A\u0438\u043D\u043E", "Forbidden Album");
    CommentPrompt prompt = CommentPromptFactory.Create(media);

    string json = prompt.MetadataPrompt["Song metadata JSON: ".Length..];
    json = json[..json.IndexOf('\n')];
    using JsonDocument parsed = JsonDocument.Parse(json);
    AssertEqual("\u041A\u0443\u043A\u0443\u0448\u043A\u0430", parsed.RootElement.GetProperty("title").GetString()!);
    AssertEqual("\u041A\u0438\u043D\u043E", parsed.RootElement.GetProperty("artist").GetString()!);
    AssertEqual(2, parsed.RootElement.EnumerateObject().Count());
    AssertFalse(prompt.MetadataPrompt.Contains("Forbidden Album", StringComparison.Ordinal));
    AssertFalse(prompt.MetadataPrompt.Contains("album", StringComparison.OrdinalIgnoreCase));
    AssertContains(prompt.SystemInstruction, "only trusted facts");
    AssertContains(prompt.SystemInstruction, "Do not quote or reproduce song lyrics");
}

static void CommentPromptCoversManualGroundingFailures()
{
    // These assertions pin the prompt rules added after manual review. They do not
    // pretend that a unit test can semantically prove future model prose is grounded.
    string prompt = CommentPromptFactory.Create(TestMedia()).SystemInstruction;

    AssertContains(
        prompt,
        "Do not ask yourself what you know about this song or artist. Treat any remembered knowledge as unavailable.");
    AssertContains(prompt, "genre, musical style, sound, vocals, voice quality, instruments, rhythm");
    AssertContains(
        prompt,
        "usual artist tendencies, nationality, origin, geography, historical context, cultural associations");
    AssertContains(prompt, "the artist's usual work");
    AssertContains(prompt, "dance floor");
    AssertContains(
        prompt,
        "If you cannot produce a safe comment using only the literal supplied metadata, do not improvise. Return exactly: FALLBACK");
}

static void PersonaPromptDocumentsCreativeBoundary()
{
    // These are prompt-contract checks, not claims that code can measure humor or factual grounding.
    string prompt = CommentPromptFactory.Create(TestMedia()).SystemInstruction;

    AssertContains(prompt, "witty, warm, self-possessed feminine companion");
    AssertContains(prompt, "deadpan, mildly sarcastic, affectionately tease");
    AssertContains(prompt, "Use Steve's name only occasionally and naturally");
    AssertContains(prompt, "I'm picturing...");
    AssertContains(prompt, "This invents an obvious scene about the wording, not a fact about a real song");
    AssertContains(prompt, "Those assert unavailable facts");
}

static async Task PersonaConfigurationReachesGenerator()
{
    var generator = new FakeCommentGenerator();
    var persona = new HostPersonaConfiguration("Zoya", "Steve");
    var service = new AiCommentService(
        generator,
        AiCommentConfiguration.Default,
        new FakeLog(),
        persona);

    await service.SelectCommentAsync(TestMedia(), true, false, CancellationToken.None);

    AssertContains(generator.LastPrompt!.SystemInstruction, "You are Zoya");
    AssertContains(generator.LastPrompt.SystemInstruction, "sitting beside Steve");
    AssertContains(generator.LastPrompt.SystemInstruction, "Do not ask yourself what you know about this song or artist");
}

static void CommentValidatorNormalizesWhitespace()
{
    const string output = "With   a title like Cochise, subtlety has clearly taken the evening off, and honestly that sounds wonderfully committed.";
    CommentValidationResult result = CommentValidator.Validate(output);
    AssertTrue(result.IsValid);
    AssertFalse(result.NormalizedText.Contains("  ", StringComparison.Ordinal));
    AssertEqual(18, result.WordCount);
}

static async Task OllamaRequestUsesSafeOptions()
{
    var handler = new RecordingHttpMessageHandler(
        HttpStatusCode.OK,
        "{\"response\":\"With a title like Cochise, subtlety has clearly taken the evening off, and honestly that sounds wonderfully committed.\",\"done\":true}");
    using var httpClient = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
    using var ollama = new OllamaClient(AiCommentConfiguration.Default, httpClient);
    CommentPrompt prompt = CommentPromptFactory.Create(new MediaSnapshot("Cochise", "Audioslave", "Must not be sent"));

    OllamaGenerationResult result = await ollama.GenerateAsync(
        prompt,
        "0",
        TimeSpan.FromSeconds(1),
        CancellationToken.None);

    AssertEqual(OllamaGenerationStatus.Success, result.Status);
    AssertEqual("http://localhost:11434/api/generate", handler.RequestUri!.AbsoluteUri);
    using JsonDocument body = JsonDocument.Parse(handler.RequestBody!);
    AssertEqual("qwen3:4b-instruct", body.RootElement.GetProperty("model").GetString()!);
    AssertFalse(body.RootElement.GetProperty("stream").GetBoolean());
    AssertFalse(body.RootElement.GetProperty("think").GetBoolean());
    AssertEqual("0", body.RootElement.GetProperty("keep_alive").GetString()!);
    AssertEqual(0.7, body.RootElement.GetProperty("options").GetProperty("temperature").GetDouble());
    AssertContains(body.RootElement.GetProperty("system").GetString()!, "You are Zoya");
    AssertContains(body.RootElement.GetProperty("system").GetString()!, "sitting beside Steve");
    AssertFalse(body.RootElement.GetProperty("prompt").GetString()!.Contains("Must not be sent", StringComparison.Ordinal));
}

static void CommentValidatorRejectsEmpty()
{
    AssertFalse(CommentValidator.Validate("  \t ").IsValid);
}

static void CommentValidatorRejectsShort()
{
    AssertFalse(CommentValidator.Validate("This title sounds ready for a little mischief tonight.").IsValid);
}

static void CommentValidatorRejectsLong()
{
    string output = string.Join(' ', Enumerable.Repeat("word", 41));
    CommentValidationResult result = CommentValidator.Validate(output);
    AssertFalse(result.IsValid);
    AssertEqual(41, result.WordCount);
}

static void CommentValidatorRejectsParagraphs()
{
    const string output = "This title walks into the room with confidence and a suspiciously tidy suitcase.\n\nThe artist name watches from the doorway, amused by everything happening inside.";
    AssertFalse(CommentValidator.Validate(output).IsValid);
}

static void CommentValidatorRejectsCliches()
{
    const string output = "What a track, with a title that strolls into the room, borrows the best chair, and refuses to explain itself to anybody.";
    AssertFalse(CommentValidator.Validate(output).IsValid);
}

static void CommentValidatorRejectsPersonaCliches()
{
    const string output = "Keep those vibes going, because this great song is another classic on the air, ready for every listener who wandered into the room.";
    AssertFalse(CommentValidator.Validate(output).IsValid);
}

static void CommentValidatorRejectsModelCommentary()
{
    const string output = "As an AI, here is the comment: this title wanders in confidently, rearranges the furniture, and leaves everyone wondering who handed it the keys.";
    AssertFalse(CommentValidator.Validate(output).IsValid);
}

static async Task ExactFallbackSelectsFixedTemplate()
{
    var generator = new FakeCommentGenerator
    {
        GenerationResult = new OllamaGenerationResult(
            OllamaGenerationStatus.Success,
            CommentValidator.FallbackSignal,
            null,
            null),
    };
    var log = new FakeLog();
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, log);

    CommentSelection result = await service.SelectCommentAsync(
        TestMedia(),
        aiEnabled: true,
        simulateAiFailure: false,
        CancellationToken.None);

    AssertEqual(CommentTextSource.FixedFallback, result.Source);
    AssertTrue(result.ModelRequestedFallback);
    AssertEqual("That was Cochise by Audioslave.", result.Text);
    AssertTrue(log.Contains("AI SAFE FALLBACK REQUESTED"));
    AssertEqual(1, generator.GenerateCalls);
    AssertEqual(1, generator.UnloadCalls);
    AssertEqual(1, generator.PsCalls);
}

static async Task FallbackSentinelIsNeverFinalText()
{
    var generator = new FakeCommentGenerator
    {
        GenerationResult = new OllamaGenerationResult(
            OllamaGenerationStatus.Success,
            "  FALLBACK\r\n",
            null,
            null),
    };
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, new FakeLog());

    CommentSelection result = await service.SelectCommentAsync(
        TestMedia(),
        aiEnabled: true,
        simulateAiFailure: false,
        CancellationToken.None);

    AssertTrue(result.ModelRequestedFallback);
    AssertFalse(string.Equals(result.Text, CommentValidator.FallbackSignal, StringComparison.Ordinal));
    AssertEqual("That was Cochise by Audioslave.", result.Text);
}

static async Task SafeFallbackIsSuccessfulGenerateOnlyResult()
{
    var generator = new FakeCommentGenerator
    {
        GenerationResult = new OllamaGenerationResult(
            OllamaGenerationStatus.Success,
            CommentValidator.FallbackSignal,
            null,
            null),
    };
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, new FakeLog());
    CommentSelection result = await service.SelectCommentAsync(
        TestMedia(),
        aiEnabled: true,
        simulateAiFailure: false,
        CancellationToken.None);

    AssertTrue(AiCommentTest.IsSafeGenerateOnlyResult(result));
}

static async Task HttpFailureUsesFallback()
{
    await AssertGenerationFailureUsesFallback(
        new OllamaGenerationResult(OllamaGenerationStatus.HttpFailure, null, "HTTP 500", null));
}

static async Task TimeoutUsesFallback()
{
    await AssertGenerationFailureUsesFallback(
        new OllamaGenerationResult(OllamaGenerationStatus.TimedOut, null, "Timed out", null));
}

static async Task MalformedResponseUsesFallback()
{
    await AssertGenerationFailureUsesFallback(
        new OllamaGenerationResult(OllamaGenerationStatus.MalformedResponse, null, "Bad JSON", null));
}

static async Task GeneratorExceptionStillUnloads()
{
    var generator = new FakeCommentGenerator { ExceptionToThrow = new InvalidOperationException("fake exception") };
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, new FakeLog());
    CommentSelection result = await service.SelectCommentAsync(TestMedia(), true, false, CancellationToken.None);

    AssertEqual(CommentTextSource.FixedFallback, result.Source);
    AssertEqual(1, generator.GenerateCalls);
    AssertEqual(1, generator.UnloadCalls);
}

static async Task ValidAiCommentStillUnloads()
{
    var generator = new FakeCommentGenerator();
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, new FakeLog());
    CommentSelection result = await service.SelectCommentAsync(TestMedia(), true, false, CancellationToken.None);

    AssertEqual(CommentTextSource.Ai, result.Source);
    AssertFalse(result.ModelRequestedFallback);
    AssertEqual(1, generator.GenerateCalls);
    AssertEqual(1, generator.UnloadCalls);
    AssertEqual(1, generator.PsCalls);
}

static async Task NoAiBypassesOllama()
{
    var generator = new FakeCommentGenerator();
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, new FakeLog());
    CommentSelection result = await service.SelectCommentAsync(TestMedia(), false, false, CancellationToken.None);

    AssertEqual(CommentTextSource.FixedFallback, result.Source);
    AssertEqual(0, generator.GenerateCalls);
    AssertEqual(0, generator.UnloadCalls);
    AssertEqual(0, generator.PsCalls);
}

static async Task SimulatedAiFailureBypassesOllama()
{
    var generator = new FakeCommentGenerator();
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, new FakeLog());
    CommentSelection result = await service.SelectCommentAsync(TestMedia(), true, true, CancellationToken.None);

    AssertEqual(CommentTextSource.FixedFallback, result.Source);
    AssertEqual(0, generator.GenerateCalls);
    AssertEqual(0, generator.UnloadCalls);
    AssertEqual(0, generator.PsCalls);
}

static void StaleTrackBlocksPreparedComment()
{
    MediaSnapshot original = TestMedia();
    AssertTrue(AiCommentSafety.CanSpeakPreparedComment(
        original,
        "FIREFOX.EXE",
        true,
        new MediaSnapshot("Cochise", "Audioslave", "Optional album"),
        true,
        GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
        true));
    AssertFalse(AiCommentSafety.CanSpeakPreparedComment(
        original,
        "firefox.exe",
        true,
        new MediaSnapshot("Know Your Enemy", "Rage Against The Machine", null),
        true,
        GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
        true));
    AssertFalse(AiCommentSafety.CanSpeakPreparedComment(
        original,
        "firefox.exe",
        true,
        original,
        true,
        GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused,
        true));
}

static async Task AssertGenerationFailureUsesFallback(OllamaGenerationResult generation)
{
    var generator = new FakeCommentGenerator { GenerationResult = generation };
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, new FakeLog());
    CommentSelection result = await service.SelectCommentAsync(TestMedia(), true, false, CancellationToken.None);

    AssertEqual(CommentTextSource.FixedFallback, result.Source);
    AssertEqual("That was Cochise by Audioslave.", result.Text);
    AssertEqual(1, generator.GenerateCalls);
    AssertEqual(1, generator.UnloadCalls);
    AssertEqual(1, generator.PsCalls);
}

static MediaSnapshot TestMedia()
{
    return new MediaSnapshot("Cochise", "Audioslave", null);
}

static AnnouncementSequenceTimings FastTimings()
{
    return new AnnouncementSequenceTimings(
        TimeSpan.FromMilliseconds(10),
        TimeSpan.FromMilliseconds(10),
        TimeSpan.FromMilliseconds(10),
        TimeSpan.FromMilliseconds(1),
        TimeSpan.FromMilliseconds(1),
        3);
}

static Task RunSynchronous(Action action)
{
    action();
    return Task.CompletedTask;
}

static IReadOnlyList<FakeSession> Select(IEnumerable<FakeSession> sessions)
{
    return FirefoxSessionSelector.FindExactMatches(sessions, session => session.SourceId);
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(actual, expected))
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}

static void AssertSame(object expected, object actual)
{
    if (!ReferenceEquals(expected, actual))
    {
        throw new InvalidOperationException("The selected session object reference changed.");
    }
}

static void AssertContains(string actual, string expected)
{
    if (!actual.Contains(expected, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"Expected text to contain: {expected}");
    }
}

static void AssertTrue(bool value)
{
    if (!value)
    {
        throw new InvalidOperationException("Expected true.");
    }
}

static void AssertFalse(bool value)
{
    if (value)
    {
        throw new InvalidOperationException("Expected false.");
    }
}

internal sealed record FakeSession(string? SourceId);

internal sealed class FakePlaybackTarget : IAnnouncementPlaybackTarget
{
    public string SourceId => FirefoxSessionSelector.VerifiedSourceId;

    public int PauseCalls { get; private set; }

    public int PlayCalls { get; private set; }

    public GlobalSystemMediaTransportControlsSessionPlaybackStatus Status { get; private set; } =
        GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

    public Task<bool> RequestPauseAsync()
    {
        PauseCalls++;
        Status = GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused;
        return Task.FromResult(true);
    }

    public Task<bool> RequestPlayAsync()
    {
        PlayCalls++;
        Status = GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
        return Task.FromResult(true);
    }

    public PlaybackStatusRead ReadPlaybackStatus()
    {
        return new PlaybackStatusRead(true, Status);
    }
}

internal sealed class FakePreparedSpeech : IPreparedAnnouncementSpeech
{
    private readonly SpeechPlaybackResult? _result;
    private readonly bool _throwCancellation;

    public FakePreparedSpeech(SpeechPlaybackResult result)
    {
        _result = result;
    }

    public FakePreparedSpeech(bool throwCancellation)
    {
        _throwCancellation = throwCancellation;
    }

    public int SpeakCalls { get; private set; }

    public Task<SpeechPlaybackResult> SpeakAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        SpeakCalls++;

        if (_throwCancellation)
        {
            throw new OperationCanceledException();
        }

        return Task.FromResult(_result!);
    }

    public void Dispose()
    {
    }
}

internal sealed class FakeLog : IAnnouncementLog
{
    private readonly List<string> _messages = new();

    public void Write(string message)
    {
        _messages.Add(message);
    }

    public void Warning(string context, Exception exception)
    {
        _messages.Add($"{context}: {exception.Message}");
    }

    public bool Contains(string text)
    {
        return _messages.Any(message => message.Contains(text, StringComparison.Ordinal));
    }
}

internal sealed class FakeCommentGenerator : ILocalCommentGenerator
{
    public string ModelName => "qwen3:4b-instruct";

    public Uri Endpoint => new("http://localhost:11434/api/");

    public int GenerateCalls { get; private set; }

    public int UnloadCalls { get; private set; }

    public int PsCalls { get; private set; }

    public Exception? ExceptionToThrow { get; init; }

    public CommentPrompt? LastPrompt { get; private set; }

    public OllamaGenerationResult GenerationResult { get; init; } = new(
        OllamaGenerationStatus.Success,
        "With a title like Cochise, subtlety has clearly taken the evening off, and honestly that sounds wonderfully committed.",
        null,
        null);

    public Task<OllamaGenerationResult> GenerateAsync(
        CommentPrompt prompt,
        string keepAlive,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        GenerateCalls++;
        LastPrompt = prompt;

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return Task.FromResult(GenerationResult);
    }

    public Task<OllamaOperationResult> UnloadAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        UnloadCalls++;
        return Task.FromResult(new OllamaOperationResult(true, "fake unload accepted"));
    }

    public Task<OllamaRunningModelsResult> GetRunningModelsAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        PsCalls++;
        return Task.FromResult(new OllamaRunningModelsResult(true, [], "fake ps succeeded"));
    }
}

internal sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _responseBody;

    public RecordingHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
    {
        _statusCode = statusCode;
        _responseBody = responseBody;
    }

    public Uri? RequestUri { get; private set; }

    public string? RequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestUri = request.RequestUri;
        RequestBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseBody),
        };
    }
}
