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
    ("radio settings validate safe defaults and bounds", () => RunSynchronous(RadioConfigurationValidatesBounds)),
    ("radio schedule requests inclusive 2-5 bounds", () => RunSynchronous(RadioScheduleUsesInclusiveBounds)),
    ("one genuine track transition increments exactly once", () => RunSynchronous(RadioTransitionIncrementsOnce)),
    ("duplicate media events do not increment radio schedule", () => RunSynchronous(RadioDuplicateDoesNotIncrement)),
    ("completed announcement resets counter with fresh interval", () => RunSynchronous(RadioCompletionChoosesFreshInterval)),
    ("Silent Mode prevents generation pause and speech", RadioSilentModePreventsPipelineWork),
    ("leaving Silent Mode starts a fresh interval", () => RunSynchronous(RadioLeavingSilentChoosesFreshInterval)),
    ("minimum track age delays automatic pause path", RadioMinimumAgeDelaysAnnouncement),
    ("track change during generation abandons pending comment", RadioTrackChangeAbandonsPipeline),
    ("track change during age wait abandons pending comment", RadioTrackChangeDuringWaitAbandonsPipeline),
    ("pending track change creates no catch-up queue", () => RunSynchronous(RadioPendingChangeCreatesNoQueue)),
    ("Paused playback prevents automatic generation and control", RadioPausedPlaybackBlocksPipeline),
    ("wrong missing and ambiguous Firefox targets block control", () => RunSynchronous(RadioTargetSelectionMustBeExact)),
    ("recent track history remains bounded", () => RunSynchronous(RadioTrackHistoryIsBounded)),
    ("recent AI comment history remains bounded", () => RunSynchronous(RadioCommentHistoryIsBounded)),
    ("clear history resets state without playback or speech", () => RunSynchronous(RadioClearHistoryIsHarmless)),
    ("repeated artist uses normalized exact matching", () => RunSynchronous(RadioRepeatedArtistUsesExactNormalization)),
    ("non-identical artist strings are not fuzzily merged", () => RunSynchronous(RadioArtistMatchingIsNotFuzzy)),
    ("radio prompt includes compact recent comments and trusted context", () => RunSynchronous(RadioPromptIncludesBoundedContext)),
    ("radio prompt preserves Unicode metadata", () => RunSynchronous(RadioPromptPreservesUnicode)),
    ("exact duplicate AI comment selects fixed fallback", RadioDuplicateAiCommentUsesFallback),
    ("radio no-AI mode bypasses Ollama", RadioNoAiBypassesOllama),
    ("radio Ollama timeout unloads and selects fallback", RadioTimeoutUsesFallback),
    ("radio maximum speech duration uses protected recovery", RadioSpeechLimitUsesProtectedRecovery),
    ("radio pipeline passes configured speech limit", RadioPipelinePassesSpeechLimit),
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

static void RadioConfigurationValidatesBounds()
{
    RadioBehaviorConfiguration.Default.Validate();

    bool minimumRejected = false;
    bool reversedRejected = false;

    try
    {
        (RadioBehaviorConfiguration.Default with { MinimumTransitionsBetweenComments = 0 }).Validate();
    }
    catch (InvalidOperationException)
    {
        minimumRejected = true;
    }

    try
    {
        (RadioBehaviorConfiguration.Default with
        {
            MinimumTransitionsBetweenComments = 5,
            MaximumTransitionsBetweenComments = 2,
        }).Validate();
    }
    catch (InvalidOperationException)
    {
        reversedRejected = true;
    }

    AssertTrue(minimumRejected);
    AssertTrue(reversedRejected);
}

static void RadioScheduleUsesInclusiveBounds()
{
    var targets = new FakeTransitionTargetSource(2, 5);
    var state = new RadioBehaviorState(RadioBehaviorConfiguration.Default, targets);
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    state.Arm(Track("A", "Artist A"), now);

    AssertEqual(2, state.ScheduledThreshold);
    AssertEqual((2, 5), targets.Requests[0]);

    state.ObserveTrack(Track("B", "Artist B"), now.AddSeconds(10));
    RadioTrackObservation due = state.ObserveTrack(Track("C", "Artist C"), now.AddSeconds(20));
    AssertTrue(due.Opportunity is not null);
    AssertTrue(state.ResolveOpportunity(due.Opportunity!.Id, true, "A sufficiently long accepted comment for history."));
    AssertEqual(5, state.ScheduledThreshold);
    AssertEqual((2, 5), targets.Requests[1]);
}

static void RadioTransitionIncrementsOnce()
{
    var state = new RadioBehaviorState(
        RadioBehaviorConfiguration.Default,
        new FakeTransitionTargetSource(5));
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    state.Arm(Track("A", "Artist A"), now);

    RadioTrackObservation result = state.ObserveTrack(Track("B", "Artist B"), now.AddSeconds(10));

    AssertEqual(RadioTrackObservationKind.Transition, result.Kind);
    AssertEqual(1, state.TransitionCounter);
    AssertEqual(1, state.TotalObservedTransitions);
}

static void RadioDuplicateDoesNotIncrement()
{
    var state = new RadioBehaviorState(
        RadioBehaviorConfiguration.Default,
        new FakeTransitionTargetSource(5));
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    state.Arm(Track("A", "Artist A"), now);
    state.ObserveTrack(Track("B", "Artist B"), now.AddSeconds(10));

    RadioTrackObservation duplicate = state.ObserveTrack(
        new MediaSnapshot("  b  ", "ARTIST   B", "Album arrived later"),
        now.AddSeconds(11));

    AssertEqual(RadioTrackObservationKind.Duplicate, duplicate.Kind);
    AssertEqual(1, state.TransitionCounter);
    AssertEqual(1, state.TotalObservedTransitions);
}

static void RadioCompletionChoosesFreshInterval()
{
    var state = new RadioBehaviorState(
        RadioBehaviorConfiguration.Default,
        new FakeTransitionTargetSource(2, 4));
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    state.Arm(Track("A", "Artist A"), now);
    state.ObserveTrack(Track("B", "Artist B"), now.AddSeconds(10));
    RadioTrackObservation due = state.ObserveTrack(Track("C", "Artist C"), now.AddSeconds(20));

    AssertTrue(state.ResolveOpportunity(
        due.Opportunity!.Id,
        announcementCompleted: true,
        "This accepted AI comment is long enough to represent a completed automatic announcement."));
    AssertEqual(0, state.TransitionCounter);
    AssertEqual(4, state.ScheduledThreshold);
    AssertEqual(1, state.RecentAcceptedAiComments.Count);
}

static async Task RadioSilentModePreventsPipelineWork()
{
    RadioBehaviorConfiguration configuration = RadioBehaviorConfiguration.Default;
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:20Z");
    RadioCommentOpportunity opportunity = TestRadioOpportunity(now.AddSeconds(-20));
    var provider = new FakeRadioCommentProvider();
    var host = new FakeRadioOpportunityHost(ValidRadioSnapshot(opportunity) with { SilentMode = true });
    var pipeline = new RadioOpportunityPipeline(
        configuration,
        provider,
        host,
        new FakeRadioClock(now),
        new FakeLog());

    RadioOpportunityResult result = await pipeline.RunAsync(opportunity, true, CancellationToken.None);

    AssertEqual(RadioOpportunityResultStatus.Abandoned, result.Status);
    AssertEqual(0, provider.Calls);
    AssertEqual(0, host.PrepareCalls);
    AssertEqual(0, host.AnnouncementCalls);
}

static void RadioLeavingSilentChoosesFreshInterval()
{
    var state = new RadioBehaviorState(
        RadioBehaviorConfiguration.Default,
        new FakeTransitionTargetSource(2, 5));
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    state.Arm(Track("A", "Artist A"), now);
    state.SetSilentMode(true);
    state.ObserveTrack(Track("B", "Artist B"), now.AddSeconds(10));

    RadioModeChange enabled = state.SetSilentMode(false);

    AssertFalse(enabled.SilentMode);
    AssertEqual(0, state.TransitionCounter);
    AssertEqual(5, state.ScheduledThreshold);
}

static async Task RadioMinimumAgeDelaysAnnouncement()
{
    DateTimeOffset started = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    var clock = new FakeRadioClock(started.AddSeconds(1));
    RadioCommentOpportunity opportunity = TestRadioOpportunity(started);
    var host = new FakeRadioOpportunityHost(ValidRadioSnapshot(opportunity))
    {
        Clock = clock,
    };
    var pipeline = new RadioOpportunityPipeline(
        RadioBehaviorConfiguration.Default,
        new FakeRadioCommentProvider(),
        host,
        clock,
        new FakeLog());

    RadioOpportunityResult result = await pipeline.RunAsync(opportunity, true, CancellationToken.None);

    AssertEqual(RadioOpportunityResultStatus.Completed, result.Status);
    AssertEqual(1, clock.Delays.Count);
    AssertEqual(TimeSpan.FromSeconds(7), clock.Delays[0]);
    AssertEqual(started.AddSeconds(8), host.AnnouncementObservedAt);
}

static async Task RadioTrackChangeAbandonsPipeline()
{
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:20Z");
    RadioCommentOpportunity opportunity = TestRadioOpportunity(now.AddSeconds(-20));
    var host = new FakeRadioOpportunityHost(ValidRadioSnapshot(opportunity));
    var provider = new FakeRadioCommentProvider
    {
        OnSelect = () => host.Snapshot = host.Snapshot with
        {
            CurrentMedia = Track("Unexpected New Track", "Different Artist"),
        },
    };
    var pipeline = new RadioOpportunityPipeline(
        RadioBehaviorConfiguration.Default,
        provider,
        host,
        new FakeRadioClock(now),
        new FakeLog());

    RadioOpportunityResult result = await pipeline.RunAsync(opportunity, true, CancellationToken.None);

    AssertEqual(RadioOpportunityResultStatus.Abandoned, result.Status);
    AssertEqual(1, provider.Calls);
    AssertEqual(0, host.PrepareCalls);
    AssertEqual(0, host.AnnouncementCalls);
}

static async Task RadioTrackChangeDuringWaitAbandonsPipeline()
{
    DateTimeOffset started = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    RadioCommentOpportunity opportunity = TestRadioOpportunity(started);
    var host = new FakeRadioOpportunityHost(ValidRadioSnapshot(opportunity));
    var clock = new FakeRadioClock(started.AddSeconds(1))
    {
        OnDelay = () => host.Snapshot = host.Snapshot with
        {
            CurrentMedia = Track("A New Track During The Wait", "Different Artist"),
        },
    };
    var pipeline = new RadioOpportunityPipeline(
        RadioBehaviorConfiguration.Default,
        new FakeRadioCommentProvider(),
        host,
        clock,
        new FakeLog());

    RadioOpportunityResult result = await pipeline.RunAsync(opportunity, true, CancellationToken.None);

    AssertEqual(RadioOpportunityResultStatus.Abandoned, result.Status);
    AssertEqual(1, clock.Delays.Count);
    AssertEqual(0, host.PrepareCalls);
    AssertEqual(0, host.AnnouncementCalls);
}

static void RadioPendingChangeCreatesNoQueue()
{
    var state = new RadioBehaviorState(
        RadioBehaviorConfiguration.Default,
        new FakeTransitionTargetSource(2, 5));
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    state.Arm(Track("A", "Artist A"), now);
    state.ObserveTrack(Track("B", "Artist B"), now.AddSeconds(10));
    RadioTrackObservation due = state.ObserveTrack(Track("C", "Artist C"), now.AddSeconds(20));
    AssertTrue(due.Opportunity is not null);

    RadioTrackObservation changedAgain = state.ObserveTrack(
        Track("D", "Artist D"),
        now.AddSeconds(30));

    AssertTrue(changedAgain.PendingOpportunityInvalidated);
    AssertTrue(changedAgain.Opportunity is null);
    AssertTrue(state.GetRuntimeState().PendingOpportunityId is null);
    AssertEqual(0, state.TransitionCounter);
    AssertEqual(5, state.ScheduledThreshold);
}

static async Task RadioPausedPlaybackBlocksPipeline()
{
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:20Z");
    RadioCommentOpportunity opportunity = TestRadioOpportunity(now.AddSeconds(-20));
    var provider = new FakeRadioCommentProvider();
    var host = new FakeRadioOpportunityHost(ValidRadioSnapshot(opportunity) with
    {
        PlaybackStatus = GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused,
    });
    var pipeline = new RadioOpportunityPipeline(
        RadioBehaviorConfiguration.Default,
        provider,
        host,
        new FakeRadioClock(now),
        new FakeLog());

    RadioOpportunityResult result = await pipeline.RunAsync(opportunity, true, CancellationToken.None);

    AssertEqual(RadioOpportunityResultStatus.Abandoned, result.Status);
    AssertEqual(0, provider.Calls);
    AssertEqual(0, host.AnnouncementCalls);
}

static void RadioTargetSelectionMustBeExact()
{
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:20Z");
    RadioCommentOpportunity opportunity = TestRadioOpportunity(now.AddSeconds(-20));
    RadioOpportunitySnapshot valid = ValidRadioSnapshot(opportunity);

    AssertFalse(RadioOpportunitySafety.Evaluate(
        opportunity,
        valid with { MatchingFirefoxSessionCount = 0 },
        now,
        TimeSpan.FromSeconds(8),
        true).CanProceed);
    AssertFalse(RadioOpportunitySafety.Evaluate(
        opportunity,
        valid with { MatchingFirefoxSessionCount = 2 },
        now,
        TimeSpan.FromSeconds(8),
        true).CanProceed);
    AssertFalse(RadioOpportunitySafety.Evaluate(
        opportunity,
        valid with { SameSelectedSession = false },
        now,
        TimeSpan.FromSeconds(8),
        true).CanProceed);
    AssertFalse(RadioOpportunitySafety.Evaluate(
        opportunity,
        valid with { SourceId = "chrome.exe" },
        now,
        TimeSpan.FromSeconds(8),
        true).CanProceed);
}

static void RadioTrackHistoryIsBounded()
{
    RadioBehaviorConfiguration configuration = RadioBehaviorConfiguration.Default with
    {
        MinimumTransitionsBetweenComments = 10,
        MaximumTransitionsBetweenComments = 10,
        RecentTrackLimit = 3,
    };
    var state = new RadioBehaviorState(configuration, new FakeTransitionTargetSource(10));
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    state.Arm(Track("A", "Artist A"), now);

    for (int index = 1; index <= 5; index++)
    {
        state.ObserveTrack(Track($"Track {index}", $"Artist {index}"), now.AddSeconds(index));
    }

    AssertEqual(3, state.RecentTracks.Count);
    AssertEqual("Track 3", state.RecentTracks[0].Title!);
    AssertEqual("Track 5", state.RecentTracks[2].Title!);
}

static void RadioCommentHistoryIsBounded()
{
    RadioBehaviorConfiguration configuration = RadioBehaviorConfiguration.Default with
    {
        MinimumTransitionsBetweenComments = 1,
        MaximumTransitionsBetweenComments = 1,
        RecentCommentLimit = 2,
    };
    var state = new RadioBehaviorState(configuration, new FakeTransitionTargetSource(1, 1, 1, 1));
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    state.Arm(Track("A", "Artist A"), now);

    for (int index = 1; index <= 3; index++)
    {
        RadioTrackObservation due = state.ObserveTrack(
            Track($"Track {index}", $"Artist {index}"),
            now.AddSeconds(index));
        state.ResolveOpportunity(due.Opportunity!.Id, true, $"Accepted AI comment number {index}");
    }

    AssertEqual(2, state.RecentAcceptedAiComments.Count);
    AssertEqual("Accepted AI comment number 2", state.RecentAcceptedAiComments[0]);
    AssertEqual("Accepted AI comment number 3", state.RecentAcceptedAiComments[1]);
}

static void RadioClearHistoryIsHarmless()
{
    RadioBehaviorConfiguration configuration = RadioBehaviorConfiguration.Default with
    {
        MinimumTransitionsBetweenComments = 1,
        MaximumTransitionsBetweenComments = 1,
    };
    var state = new RadioBehaviorState(configuration, new FakeTransitionTargetSource(1, 1));
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    state.Arm(Track("A", "Artist A"), now);
    RadioTrackObservation due = state.ObserveTrack(Track("B", "Artist B"), now.AddSeconds(10));
    AssertTrue(due.Opportunity is not null);

    bool invalidated = state.ClearRecentHistory();

    AssertTrue(invalidated);
    AssertEqual(0, state.RecentTracks.Count);
    AssertEqual(0, state.RecentAcceptedAiComments.Count);
    AssertEqual(0, state.TransitionCounter);
    AssertTrue(state.GetRuntimeState().PendingOpportunityId is null);
}

static void RadioRepeatedArtistUsesExactNormalization()
{
    var history = new RadioHistory(10, 6);
    history.AddTrack(Track("One", "  Bee   Gees "));
    history.AddTrack(Track("Two", "bee gees"));

    IReadOnlyList<string> observations = history.BuildTrustedObservations(Track("Two", "BEE GEES"));

    AssertEqual(1, observations.Count);
    AssertContains(observations[0], "appeared 2 times");
}

static void RadioArtistMatchingIsNotFuzzy()
{
    var history = new RadioHistory(10, 6);
    history.AddTrack(Track("One", "Artist"));
    history.AddTrack(Track("Two", "Artist feat. X"));

    IReadOnlyList<string> observations = history.BuildTrustedObservations(Track("Two", "Artist feat. X"));

    AssertEqual(0, observations.Count);
}

static void RadioPromptIncludesBoundedContext()
{
    DateTimeOffset started = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    var context = new RadioCommentContext(
        Track("Previous Title", "Previous Artist"),
        Track("Current Title", "Current Artist"),
        started,
        ["The exact artist string 'Current Artist' has appeared 2 times in the last 6 observed tracks."],
        ["A recent opening that should not be copied exactly."]);

    CommentPrompt prompt = CommentPromptFactory.CreateForRadio(context, HostPersonaConfiguration.Default);
    const string prefix = "Automatic radio context JSON: ";
    string json = prompt.MetadataPrompt[prefix.Length..];
    json = json[..json.IndexOf('\n')];
    using JsonDocument parsed = JsonDocument.Parse(json);

    AssertEqual("Previous Title", parsed.RootElement.GetProperty("previousTitle").GetString()!);
    AssertEqual("Current Artist", parsed.RootElement.GetProperty("currentArtist").GetString()!);
    AssertEqual(1, parsed.RootElement.GetProperty("trustedListeningObservations").GetArrayLength());
    AssertEqual(1, parsed.RootElement.GetProperty("recentAcceptedComments").GetArrayLength());
    AssertEqual(6, parsed.RootElement.EnumerateObject().Count());
    AssertContains(prompt.MetadataPrompt, "Comment primarily on the previous track");
    AssertContains(prompt.SystemInstruction, "Recent accepted comments are style-avoidance examples");
    AssertFalse(prompt.MetadataPrompt.Contains("album", StringComparison.OrdinalIgnoreCase));
}

static void RadioPromptPreservesUnicode()
{
    var context = new RadioCommentContext(
        Track("Кукушка", "Кино"),
        Track("Дай Дай", "Русский Оркестр"),
        DateTimeOffset.Parse("2026-08-07T12:00:00Z"),
        [],
        []);
    CommentPrompt prompt = CommentPromptFactory.CreateForRadio(context, HostPersonaConfiguration.Default);
    const string prefix = "Automatic radio context JSON: ";
    string json = prompt.MetadataPrompt[prefix.Length..];
    json = json[..json.IndexOf('\n')];
    using JsonDocument parsed = JsonDocument.Parse(json);

    AssertEqual("Кукушка", parsed.RootElement.GetProperty("previousTitle").GetString()!);
    AssertEqual("Кино", parsed.RootElement.GetProperty("previousArtist").GetString()!);
    AssertEqual("Дай Дай", parsed.RootElement.GetProperty("currentTitle").GetString()!);
}

static async Task RadioDuplicateAiCommentUsesFallback()
{
    const string generated = "With a title like Cochise, subtlety has clearly taken the evening off, and honestly that sounds wonderfully committed.";
    var generator = new FakeCommentGenerator();
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, new FakeLog());
    var context = new RadioCommentContext(
        TestMedia(),
        Track("Next Track", "Next Artist"),
        DateTimeOffset.Now,
        [],
        ["  with A TITLE like cochise, subtlety has clearly taken the evening off, and honestly that sounds wonderfully committed.  "]);

    CommentSelection result = await service.SelectRadioCommentAsync(context, true, CancellationToken.None);

    AssertEqual(CommentTextSource.FixedFallback, result.Source);
    AssertEqual("That was Cochise by Audioslave.", result.Text);
    AssertEqual(1, generator.GenerateCalls);
    AssertEqual(1, generator.UnloadCalls);
    AssertTrue(AiCommentService.IsExactRecentComment(generated, context.RecentAcceptedAiComments));
}

static async Task RadioNoAiBypassesOllama()
{
    var generator = new FakeCommentGenerator();
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, new FakeLog());
    var context = new RadioCommentContext(
        TestMedia(),
        Track("Next Track", "Next Artist"),
        DateTimeOffset.Now,
        [],
        []);

    CommentSelection result = await service.SelectRadioCommentAsync(context, false, CancellationToken.None);

    AssertEqual(CommentTextSource.FixedFallback, result.Source);
    AssertEqual(0, generator.GenerateCalls);
    AssertEqual(0, generator.UnloadCalls);
    AssertEqual(0, generator.PsCalls);
}

static async Task RadioTimeoutUsesFallback()
{
    var generator = new FakeCommentGenerator
    {
        GenerationResult = new OllamaGenerationResult(
            OllamaGenerationStatus.TimedOut,
            null,
            "Timed out",
            null),
    };
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, new FakeLog());
    var context = new RadioCommentContext(
        TestMedia(),
        Track("Next Track", "Next Artist"),
        DateTimeOffset.Now,
        [],
        []);

    CommentSelection result = await service.SelectRadioCommentAsync(context, true, CancellationToken.None);

    AssertEqual(CommentTextSource.FixedFallback, result.Source);
    AssertEqual("That was Cochise by Audioslave.", result.Text);
    AssertEqual(1, generator.GenerateCalls);
    AssertEqual(1, generator.UnloadCalls);
    AssertEqual(1, generator.PsCalls);
}

static async Task RadioSpeechLimitUsesProtectedRecovery()
{
    RadioBehaviorConfiguration configuration = RadioBehaviorConfiguration.Default;
    var target = new FakePlaybackTarget();
    var speech = new FakePreparedSpeech(new SpeechPlaybackResult(SpeechPlaybackStatus.TimedOut));
    var log = new FakeLog();
    AnnouncementSequenceTimings timings = FastTimings() with
    {
        SpeechTimeout = configuration.MaximumSpeechDuration,
    };
    var sequence = new AnnouncementControlSequence(log, timings);

    AnnouncementSequenceResult result = await sequence.RunAsync(
        target,
        speech,
        false,
        CancellationToken.None);

    AssertFalse(result.TestPassed);
    AssertTrue(result.PlaybackRestored);
    AssertEqual(1, target.PauseCalls);
    AssertEqual(1, target.PlayCalls);
    AssertEqual(configuration.MaximumSpeechDuration, speech.LastTimeout);
    AssertTrue(log.Contains("speech time limit reached"));
}

static async Task RadioPipelinePassesSpeechLimit()
{
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:20Z");
    RadioCommentOpportunity opportunity = TestRadioOpportunity(now.AddSeconds(-20));
    var host = new FakeRadioOpportunityHost(ValidRadioSnapshot(opportunity));
    var pipeline = new RadioOpportunityPipeline(
        RadioBehaviorConfiguration.Default,
        new FakeRadioCommentProvider(),
        host,
        new FakeRadioClock(now),
        new FakeLog());

    RadioOpportunityResult result = await pipeline.RunAsync(opportunity, true, CancellationToken.None);

    AssertEqual(RadioOpportunityResultStatus.Completed, result.Status);
    AssertEqual(TimeSpan.FromSeconds(25), host.MaximumSpeechDuration);
}

static MediaSnapshot Track(string title, string artist)
{
    return new MediaSnapshot(title, artist, null);
}

static RadioCommentOpportunity TestRadioOpportunity(DateTimeOffset currentTrackStartedAt)
{
    var context = new RadioCommentContext(
        Track("Previous Track", "Previous Artist"),
        Track("Current Track", "Current Artist"),
        currentTrackStartedAt,
        [],
        []);
    return new RadioCommentOpportunity(1, context);
}

static RadioOpportunitySnapshot ValidRadioSnapshot(RadioCommentOpportunity opportunity)
{
    return new RadioOpportunitySnapshot(
        Armed: true,
        SilentMode: false,
        OpportunityStillPending: true,
        SessionListReadSucceeded: true,
        MatchingFirefoxSessionCount: 1,
        SameSelectedSession: true,
        SourceId: "firefox.exe",
        SourceReadSucceeded: true,
        CurrentMedia: opportunity.Context.CurrentTrack,
        MediaReadSucceeded: true,
        PlaybackStatus: GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
        PlaybackReadSucceeded: true);
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

    public TimeSpan? LastTimeout { get; private set; }

    public Task<SpeechPlaybackResult> SpeakAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        SpeakCalls++;
        LastTimeout = timeout;

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

internal sealed class FakeTransitionTargetSource : ITransitionTargetSource
{
    private readonly Queue<int> _values;
    private int _lastValue;

    public FakeTransitionTargetSource(params int[] values)
    {
        if (values.Length == 0)
        {
            throw new ArgumentException("At least one transition target is required.", nameof(values));
        }

        _values = new Queue<int>(values);
        _lastValue = values[^1];
    }

    public List<(int Minimum, int Maximum)> Requests { get; } = [];

    public int NextInclusive(int minimum, int maximum)
    {
        Requests.Add((minimum, maximum));
        if (_values.Count > 0)
        {
            _lastValue = _values.Dequeue();
        }

        return _lastValue;
    }
}

internal sealed class FakeRadioClock : IRadioClock
{
    public FakeRadioClock(DateTimeOffset now)
    {
        Now = now;
    }

    public DateTimeOffset Now { get; private set; }

    public List<TimeSpan> Delays { get; } = [];

    public Action? OnDelay { get; init; }

    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Delays.Add(delay);
        Now += delay;
        OnDelay?.Invoke();
        return Task.CompletedTask;
    }
}

internal sealed class FakeRadioCommentProvider : IRadioCommentProvider
{
    public int Calls { get; private set; }

    public Action? OnSelect { get; init; }

    public CommentSelection Selection { get; init; } = new(
        "That title has arrived with enough confidence to rearrange the furniture, Steve, and frankly I admire the paperwork-free commitment.",
        CommentTextSource.Ai,
        19,
        "fake accepted AI comment",
        null,
        false);

    public Task<CommentSelection> SelectAsync(
        RadioCommentContext context,
        bool aiEnabled,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        OnSelect?.Invoke();
        return Task.FromResult(Selection);
    }
}

internal sealed class FakeRadioOpportunityHost : IRadioOpportunityHost
{
    private readonly FakePreparedSpeech _speech = new(
        new SpeechPlaybackResult(SpeechPlaybackStatus.Completed));

    public FakeRadioOpportunityHost(RadioOpportunitySnapshot snapshot)
    {
        Snapshot = snapshot;
    }

    public RadioOpportunitySnapshot Snapshot { get; set; }

    public int SnapshotReads { get; private set; }

    public int PrepareCalls { get; private set; }

    public int AnnouncementCalls { get; private set; }

    public TimeSpan? MaximumSpeechDuration { get; private set; }

    public DateTimeOffset? AnnouncementObservedAt { get; private set; }

    public FakeRadioClock? Clock { get; set; }

    public Task<RadioOpportunitySnapshot> ReadSnapshotAsync(
        long opportunityId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SnapshotReads++;
        return Task.FromResult(Snapshot);
    }

    public Task<IPreparedAnnouncementSpeech> PrepareSpeechAsync(
        string text,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PrepareCalls++;
        return Task.FromResult<IPreparedAnnouncementSpeech>(_speech);
    }

    public Task<AnnouncementSequenceResult> RunAnnouncementAsync(
        IPreparedAnnouncementSpeech preparedSpeech,
        TimeSpan maximumSpeechDuration,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AnnouncementCalls++;
        MaximumSpeechDuration = maximumSpeechDuration;
        AnnouncementObservedAt = Clock?.Now;
        return Task.FromResult(new AnnouncementSequenceResult(
            TestPassed: true,
            PauseVerified: true,
            PlaybackRestored: true,
            RecoveryNeeded: false,
            SimulatedFailure: false));
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
