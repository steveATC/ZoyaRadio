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
    where T : IEquatable<T>
{
    if (!actual.Equals(expected))
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
