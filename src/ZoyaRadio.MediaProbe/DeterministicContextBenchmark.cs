using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Text.RegularExpressions;

namespace ZoyaRadio.MediaProbe;

internal sealed record DeterministicContextBenchmarkManifest(
    string ManifestVersion,
    int SamplesPerCondition,
    IReadOnlyList<ZoyaPersonaMemory> PersonaMemories,
    IReadOnlyList<DeterministicContextScenario> Scenarios);

internal sealed record ZoyaPersonaMemory(
    string Id,
    string CanonicalText,
    IReadOnlyList<string> RelevanceTags,
    bool Enabled,
    string Classification,
    string? AllowedFirstPersonPattern = null);

internal sealed record DeterministicContextScenario(
    string Id,
    string Description,
    string Title,
    string Artist,
    string Condition,
    string HookKind,
    string AnchorText,
    string HookDetail,
    string SourceName,
    string SourceUrl,
    string EditorialValue,
    IReadOnlyList<string> AllowedEntities,
    string? PersonaMemoryId)
{
    // The benchmark has no live identity lookup: these exact owner-curated fields are the verified identity.
    public string VerifiedIdentity => $"curated-confirmed:{Artist}::{Title}";

    public bool IsEditoriallyUseful => string.Equals(EditorialValue, "useful", StringComparison.Ordinal);
}

internal sealed record DeterministicLinkValidationResult(
    bool IsValid,
    bool IsFallback,
    string NormalizedText,
    int WordCount,
    string Reason);

internal sealed record DeterministicContextComposition(
    string FinalText,
    string OutputKind,
    string Reason);

internal sealed record DeterministicContextBenchmarkSample(
    string ScenarioId,
    string Condition,
    string? PersonaMemoryId,
    int SampleNumber,
    string PromptSha256,
    string? GeneratedLink,
    string FinalComment,
    string OutputKind,
    double LatencyMilliseconds,
    string ValidatorOutcome);

internal static class DeterministicContextBenchmarkManifestLoader
{
    private const string ResourceSuffix = "Prompts.Milestone9C4DeterministicContextScenarios.json";
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static DeterministicContextBenchmarkManifest Load()
    {
        Assembly assembly = typeof(DeterministicContextBenchmarkManifestLoader).Assembly;
        string? resource = assembly.GetManifestResourceNames().SingleOrDefault(name => name.EndsWith(ResourceSuffix, StringComparison.Ordinal));
        if (resource is null)
        {
            throw new InvalidOperationException("The M9C.4 deterministic-context scenario manifest is missing.");
        }

        using Stream stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException("The M9C.4 deterministic-context scenario manifest could not be opened.");
        DeterministicContextBenchmarkManifest manifest = JsonSerializer.Deserialize<DeterministicContextBenchmarkManifest>(stream, Options)
            ?? throw new InvalidOperationException("The M9C.4 deterministic-context scenario manifest is empty.");
        Validate(manifest);
        return manifest;
    }

    internal static void Validate(DeterministicContextBenchmarkManifest manifest)
    {
        if (manifest.ManifestVersion != "1.4" || manifest.SamplesPerCondition != 3 || manifest.Scenarios.Count != 20 || manifest.PersonaMemories.Count is < 4 or > 8)
        {
            throw new InvalidOperationException("The M9C.4 corpus must contain 20 conditions, three samples each, and 4-8 persona memories.");
        }

        if (manifest.Scenarios.Select(scenario => scenario.Id).Distinct(StringComparer.Ordinal).Count() != manifest.Scenarios.Count
            || manifest.PersonaMemories.Select(memory => memory.Id).Distinct(StringComparer.Ordinal).Count() != manifest.PersonaMemories.Count
            || manifest.PersonaMemories.Any(memory => string.IsNullOrWhiteSpace(memory.Id)
                || string.IsNullOrWhiteSpace(memory.CanonicalText)
                || !memory.Enabled
                || memory.Classification != "FictionalPersonaFact"))
        {
            throw new InvalidOperationException("M9C.4 requires unique enabled FictionalPersonaFact persona memories.");
        }

        var memoryIds = manifest.PersonaMemories.Select(memory => memory.Id).ToHashSet(StringComparer.Ordinal);
        if (manifest.Scenarios.Any(scenario => string.IsNullOrWhiteSpace(scenario.Id)
            || string.IsNullOrWhiteSpace(scenario.Title)
            || string.IsNullOrWhiteSpace(scenario.Artist)
            || string.IsNullOrWhiteSpace(scenario.AnchorText)
            || string.IsNullOrWhiteSpace(scenario.HookDetail)
            || (scenario.EditorialValue is not "useful" and not "weak")
            || scenario.AllowedEntities.Count == 0
            || !IsApprovedSourceUrl(scenario.SourceUrl)
            || ContainsRawPageContent(scenario.AnchorText)
            || ContainsRawPageContent(scenario.HookDetail)
            || (scenario.PersonaMemoryId is not null && !memoryIds.Contains(scenario.PersonaMemoryId))))
        {
            throw new InvalidOperationException("M9C.4 conditions require exact metadata, a clean deterministic anchor, a direct approved source URL, and an optional known persona memory.");
        }

        string[] requiredArtists =
        [
            "Lynyrd Skynyrd", "Black Sabbath", "Ronnie James Dio", "Jerry Reed", "Megadeth", "Metallica", "System of a Down", "Bee Gees",
            "Black Label Society", "Tenacious D", "Rage Against the Machine", "Rammstein", "Shakira", "Elis Regina", "Raul Seixas", "Johnny Cash",
        ];
        if (requiredArtists.Any(artist => !manifest.Scenarios.Any(scenario => string.Equals(scenario.Artist, artist, StringComparison.Ordinal))))
        {
            throw new InvalidOperationException("The M9C.4 owner-familiar artist corpus is incomplete.");
        }
    }

    internal static bool IsApprovedSourceUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        return uri.Host.Equals("en.wikipedia.org", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("www.grammy.com", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("www.universalmusic.com.br", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("www.johnnycash.com", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsRawPageContent(string value)
        => value.Contains('<') || value.Contains('>') || value.Contains("<html", StringComparison.OrdinalIgnoreCase);
}

internal static class DeterministicContextLinkPromptFactory
{
    private const string ResourceSuffix = "Prompts.Milestone9C4DeterministicLinkPrompt.txt";
    private static readonly Lazy<string> Template = new(LoadTemplate);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    public static CommentPrompt Create(DeterministicContextScenario scenario, ZoyaPersonaMemory? personaMemory, IReadOnlyList<string> recentLinks)
    {
        object persona = personaMemory is null
            ? new { selected = false }
            : new
            {
                selected = true,
                id = personaMemory.Id,
                classification = personaMemory.Classification,
                canonicalText = personaMemory.CanonicalText,
                relevanceTags = personaMemory.RelevanceTags,
            };
        var card = new
        {
            deterministicAnchor = scenario.AnchorText,
            hookDetail = scenario.HookDetail,
            sourceName = scenario.SourceName,
            sourceUrl = scenario.SourceUrl,
            editorialValue = scenario.EditorialValue,
            allowedEntities = scenario.AllowedEntities,
        };
        string metadata = JsonSerializer.Serialize(new { title = scenario.Title, artist = scenario.Artist }, JsonOptions);
        return new CommentPrompt(
            Template.Value,
            "<EXACT_METADATA_JSON>\n" + metadata
            + "\n</EXACT_METADATA_JSON>\n<DETERMINISTIC_FACTUAL_ANCHOR>\n" + scenario.AnchorText
            + "\n</DETERMINISTIC_FACTUAL_ANCHOR>\n<SOURCE_BACKED_HOOK_JSON>\n" + JsonSerializer.Serialize(card, JsonOptions)
            + "\n</SOURCE_BACKED_HOOK_JSON>\n<SELECTED_FICTIONAL_PERSONA_MEMORY_JSON>\n" + JsonSerializer.Serialize(persona, JsonOptions)
            + "\n</SELECTED_FICTIONAL_PERSONA_MEMORY_JSON>\n<RECENT_ACCEPTED_LINKS_JSON>\n" + JsonSerializer.Serialize(recentLinks, JsonOptions)
            + "\n</RECENT_ACCEPTED_LINKS_JSON>\nWrite the subjective link now.");
    }

    private static string LoadTemplate()
    {
        Assembly assembly = typeof(DeterministicContextLinkPromptFactory).Assembly;
        string? resource = assembly.GetManifestResourceNames().SingleOrDefault(name => name.EndsWith(ResourceSuffix, StringComparison.Ordinal));
        if (resource is null)
        {
            throw new InvalidOperationException("The M9C.4 deterministic link prompt resource is missing.");
        }

        using Stream stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException("The M9C.4 deterministic link prompt resource could not be opened.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Trim();
    }
}

internal static class DeterministicContextLinkValidator
{
    private static readonly string[] BannedPhrases =
    [
        "i'm picturing", "i\\u2019m picturing", "i'm seeing", "i\\u2019m seeing", "i'm choosing to interpret", "i\\u2019m choosing to interpret",
        "as an ai", "turn it up", "stay tuned", "what a track", "absolute banger", "timeless classic",
    ];
    private static readonly string[] ReactionSignals =
    [
        "imagine", "hell of", "not exactly", "that's", "that is", "someone", "somebody", "wonderfully", "pretty", "almost", "like ",
        "feels", "feel ", "clearly", "apparently", "one way", "what a", "strange", "odd", "petty", "committed", "perfect", "bad haircut",
        "room", "place", "wild", "absurd", "ridiculous", "subtle", "of a decision",
    ];
    private static readonly string[] SoundFactTerms =
    [
        "voice", "vocals", "melody", "beat", "rhythm", "tempo", "guitar", "drums", "instrument", "production", "genre", "lyrics", "groove", "gospel", "dance floor",
    ];
    private static readonly HashSet<string> CommonSentenceStarts = new(
        ["I", "The", "A", "An", "This", "That", "It", "And", "But", "Zoya", "Apparently", "Clearly", "Honestly", "Paperwork", "Well", "Nothing", "Somehow", "Why", "They", "He", "She", "Hell", "Imagine", "Someone", "Somebody", "Not", "No", "Maybe", "Perhaps", "What", "There", "One", "Talk", "Tell", "Sure", "Really", "Now", "Even", "Funny"],
        StringComparer.Ordinal);
    private static readonly HashSet<string> CommonPhraseTokens = new(
        ["THE", "AND", "THAT", "THIS", "WITH", "FROM", "LIKE", "FEELS", "FEEL", "SONG", "TRACK", "RECORD", "MUSIC", "ABOUT", "JUST", "STILL", "THERE", "THEY", "THEIR", "WERE", "WAS", "HAVE", "HAD", "NOT", "FOR", "ONE"],
        StringComparer.Ordinal);

    public static DeterministicLinkValidationResult Validate(
        string? response,
        DeterministicContextScenario scenario,
        ZoyaPersonaMemory? personaMemory,
        IEnumerable<string>? recentAcceptedLinks = null)
    {
        if (CommentValidator.IsFallbackSignal(response))
        {
            return new DeterministicLinkValidationResult(true, true, CommentValidator.FallbackSignal, 0, "Safe FALLBACK sentinel.");
        }

        string raw = response ?? string.Empty;
        string normalized = CommentValidator.NormalizeWhitespace(raw);
        int words = WordCount(normalized);
        if (words < 3 || words > 22 || SentenceCount(normalized) > 1 || raw.Contains('\r') || raw.Contains('\n'))
        {
            return Invalid(words, "The generated reaction must be one 3-22 word sentence.");
        }

        if (ContainsBannedCliche(normalized)
            || ContainsUnlicensedFirstPersonExperience(normalized, personaMemory)
            || ContainsCollectiveParticipation(normalized)
            || ContainsUnsupportedAbsolute(normalized)
            || ContainsUnsupportedMotive(normalized)
            || ContainsUnsupportedSoundFact(normalized)
            || ContainsUnapprovedNumber(normalized, scenario, personaMemory)
            || ContainsUnapprovedProperNoun(normalized, scenario, personaMemory))
        {
            return Invalid(words, "The generated reaction introduced an unsupported claim, biography, entity, or prohibited radio cliche.");
        }

        bool editorialSignal = HasEditorialSignal(normalized);
        if (LooksLikeAnchorRecitation(normalized, scenario.AnchorText)
            || LooksLikeAnchorRecitation(normalized, scenario.HookDetail)
            || (TokenOverlap(normalized, scenario.AnchorText) >= 0.50 && !editorialSignal)
            || (TokenOverlap(normalized, scenario.HookDetail) >= 0.50 && !editorialSignal))
        {
            return Invalid(words, "The generated reaction repeated factual material without adding a clear host reaction.");
        }

        if (!editorialSignal)
        {
            return Invalid(words, "The generated text did not contain a clear subjective host reaction.");
        }

        if (recentAcceptedLinks?.Any(previous => IsNearDuplicate(normalized, previous) || SharesDistinctivePhrase(normalized, previous)) == true)
        {
            return Invalid(words, "The generated reaction repeated a recent distinctive phrase or structure.");
        }

        return new DeterministicLinkValidationResult(true, false, normalized, words, "Accepted bounded host reaction.");
    }

    private static DeterministicLinkValidationResult Invalid(int words, string reason)
        => new(false, false, CommentValidator.FallbackSignal, words, reason);

    private static bool ContainsBannedCliche(string value)
        => BannedPhrases.Any(phrase => value.Contains(phrase.Replace("\\u2019", "\u2019"), StringComparison.OrdinalIgnoreCase));

    private static bool ContainsUnlicensedFirstPersonExperience(string value, ZoyaPersonaMemory? personaMemory)
    {
        bool experience = Regex.IsMatch(
            value,
            "\\b(?:I|we)\\s+(?:remember(?:ed)?|saw|seen|heard|was\\s+there|went|visited|lived|worked|was\\s+born|owned|bought|used\\s+to|met|watched|attended|kept|smelled|felt)\\b|\\b(?:my|our)\\s+(?:memory|memories|copy|collection|ticket|room|home)\\b|\\bthat\\s+reminds\\s+me\\s+of\\s+when\\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!experience)
        {
            return false;
        }

        return personaMemory?.AllowedFirstPersonPattern is null
            || !Regex.IsMatch(value, personaMemory.AllowedFirstPersonPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static bool ContainsCollectiveParticipation(string value)
        => Regex.IsMatch(value, "\\b(?:we|us|our)\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static bool ContainsUnsupportedAbsolute(string value)
        => Regex.IsMatch(value, "\\b(?:never|always|everyone|nobody|every(?:one|thing)?|only|first\\s+ever|last\\s+ever)\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static bool ContainsUnsupportedMotive(string value)
        => Regex.IsMatch(
            value,
            "\\b(?:they|he|she|it)\\s+(?:did|does|wanted|meant|tried|were\\s+trying|was\\s+trying)\\b|\\b(?:the\\s+)?(?:name\\s+change|decision|title)\\s+(?:was|were)\\s+always\\b|\\b(?:it|they|he|she)\\s+(?:was|were)\\s+(?:a|an)\\s+(?:protest|rebellion|statement|response)\\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static bool ContainsUnsupportedSoundFact(string value)
        => SoundFactTerms.Any(term => Regex.IsMatch(value, $"\\b{Regex.Escape(term)}\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
           || Regex.IsMatch(value, "\\bheavy\\s+(riff|groove|song|music)\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static bool ContainsUnapprovedNumber(string link, DeterministicContextScenario scenario, ZoyaPersonaMemory? personaMemory)
    {
        string allowed = scenario.AnchorText + " " + scenario.HookDetail + " " + string.Join(" ", scenario.AllowedEntities) + " " + personaMemory?.CanonicalText;
        return Regex.Matches(link, "\\d+", RegexOptions.CultureInvariant)
            .Any(match => !allowed.Contains(match.Value, StringComparison.Ordinal));
    }

    private static bool ContainsUnapprovedProperNoun(string link, DeterministicContextScenario scenario, ZoyaPersonaMemory? personaMemory)
    {
        HashSet<string> allowed = Tokens(scenario.Title + " " + scenario.Artist + " " + scenario.AnchorText + " " + scenario.HookDetail + " " + string.Join(" ", scenario.AllowedEntities) + " " + personaMemory?.CanonicalText);
        foreach (Match match in Regex.Matches(link, "\\b[\\p{Lu}][\\p{L}\\p{M}'\\u2019-]+", RegexOptions.CultureInvariant))
        {
            if (!allowed.Contains(match.Value.ToUpperInvariant()) && !CommonSentenceStarts.Contains(match.Value))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasEditorialSignal(string value)
        => value.Contains('?') || value.Contains('!') || ReactionSignals.Any(signal => value.Contains(signal, StringComparison.OrdinalIgnoreCase));

    private static bool LooksLikeAnchorRecitation(string response, string anchor)
        => NormalizeForComparison(response).Contains(NormalizeForComparison(anchor), StringComparison.Ordinal)
            || TokenOverlap(response, anchor) >= 0.85;

    private static bool IsNearDuplicate(string left, string right)
        => string.Equals(NormalizeForComparison(left), NormalizeForComparison(right), StringComparison.Ordinal)
            || TokenOverlap(left, right) >= 0.75;

    private static bool SharesDistinctivePhrase(string left, string right)
    {
        HashSet<string> rightPhrases = DistinctivePhrases(right);
        return DistinctivePhrases(left).Overlaps(rightPhrases);
    }

    private static HashSet<string> DistinctivePhrases(string value)
    {
        string[] tokens = Regex.Matches(NormalizeForComparison(value), "[\\p{L}\\p{N}]+", RegexOptions.CultureInvariant)
            .Select(match => match.Value)
            .Where(token => !CommonPhraseTokens.Contains(token))
            .ToArray();
        var phrases = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index + 1 < tokens.Length; index++)
        {
            if (tokens[index].Length >= 4 && tokens[index + 1].Length >= 4 && tokens[index].Length + tokens[index + 1].Length >= 13)
            {
                phrases.Add(tokens[index] + " " + tokens[index + 1]);
            }
        }

        return phrases;
    }

    private static double TokenOverlap(string left, string right)
    {
        HashSet<string> leftTokens = Tokens(left);
        HashSet<string> rightTokens = Tokens(right);
        if (leftTokens.Count == 0 || rightTokens.Count == 0)
        {
            return 0;
        }

        return leftTokens.Intersect(rightTokens, StringComparer.Ordinal).Count() / (double)Math.Min(leftTokens.Count, rightTokens.Count);
    }

    private static HashSet<string> Tokens(string value)
        => Regex.Matches(NormalizeForComparison(value), "[\\p{L}\\p{N}]+", RegexOptions.CultureInvariant)
            .Select(match => match.Value)
            .Where(token => token.Length > 1)
            .ToHashSet(StringComparer.Ordinal);

    private static int SentenceCount(string value)
        => Regex.Matches(value, "[.!?]+", RegexOptions.CultureInvariant).Count;

    private static string NormalizeForComparison(string value)
        => Regex.Replace(value.ToUpperInvariant(), "[^\\p{L}\\p{N}]+", " ").Trim();

    private static int WordCount(string value)
        => string.IsNullOrWhiteSpace(value) ? 0 : value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
}

internal static class DeterministicContextComposer
{
    public static DeterministicContextComposition Compose(DeterministicContextScenario scenario, DeterministicLinkValidationResult validation)
    {
        if (validation.IsValid && !validation.IsFallback)
        {
            string composed = EnsureTerminalPunctuation(scenario.AnchorText) + " " + EnsureTerminalPunctuation(validation.NormalizedText);
            if (WordCount(composed) <= 40)
            {
                return new DeterministicContextComposition(composed, "anchor-plus-link", validation.Reason);
            }

            validation = validation with { IsValid = false, Reason = "The composed anchor and reaction exceed the 40-word benchmark limit." };
        }

        string reason = scenario.IsEditoriallyUseful
            ? validation.Reason + " Useful hooks require a valid host reaction; anchor-only output is intentionally suppressed."
            : validation.Reason + " This weak hook may safely choose FALLBACK.";
        return new DeterministicContextComposition(CommentValidator.FallbackSignal, "fallback", reason);
    }

    private static string EnsureTerminalPunctuation(string value)
        => value.EndsWith(".", StringComparison.Ordinal) || value.EndsWith("!", StringComparison.Ordinal) || value.EndsWith("?", StringComparison.Ordinal)
            ? value
            : value + ".";

    private static int WordCount(string value)
        => string.IsNullOrWhiteSpace(value) ? 0 : value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
}

internal sealed class DeterministicContextBenchmarkRunner
{
    private readonly ILocalCommentGenerator _generator;
    private readonly AiCommentConfiguration _configuration;

    public DeterministicContextBenchmarkRunner(ILocalCommentGenerator generator, AiCommentConfiguration configuration)
    {
        _generator = generator;
        _configuration = configuration;
        _configuration.Validate();
    }

    public async Task<MusicContextBenchmarkRunResult> RunAsync(string repositoryRoot, CancellationToken cancellationToken)
    {
        DeterministicContextBenchmarkManifest manifest = DeterministicContextBenchmarkManifestLoader.Load();
        OllamaInstalledModelsResult installed = await _generator.GetInstalledModelsAsync(_configuration.LocalOperationTimeout, cancellationToken);
        if (!installed.Success || !installed.Models.Any(IsSelectedModel))
        {
            return new MusicContextBenchmarkRunResult(false, "M9C.4 benchmark blocked: approved local qwen3:4b-instruct is not available. Nothing was downloaded.", null, 0, 0, 0, false, null);
        }

        string directory = Path.Combine(repositoryRoot, "artifacts", "m9c", DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(directory);
        var samples = new List<DeterministicContextBenchmarkSample>();
        var recentAcceptedLinks = new List<string>();
        int attempted = 0;
        bool unloadRequested = false;
        bool? unloadVerified = null;
        try
        {
            foreach (DeterministicContextScenario scenario in manifest.Scenarios)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ZoyaPersonaMemory? persona = scenario.PersonaMemoryId is null
                    ? null
                    : manifest.PersonaMemories.Single(memory => string.Equals(memory.Id, scenario.PersonaMemoryId, StringComparison.Ordinal));
                CommentPrompt prompt = DeterministicContextLinkPromptFactory.Create(scenario, persona, recentAcceptedLinks);
                for (int sampleNumber = 1; sampleNumber <= manifest.SamplesPerCondition; sampleNumber++)
                {
                    attempted++;
                    Console.WriteLine($"M9C.4 benchmark {attempted}/{manifest.Scenarios.Count * manifest.SamplesPerCondition}: {scenario.Id}, sample {sampleNumber}");
                    samples.Add(await GenerateAsync(scenario, persona, sampleNumber, prompt, recentAcceptedLinks, cancellationToken));
                }
            }
        }
        finally
        {
            unloadRequested = true;
            await _generator.UnloadAsync(_configuration.LocalOperationTimeout, CancellationToken.None);
            OllamaRunningModelsResult running = await _generator.GetRunningModelsAsync(_configuration.LocalOperationTimeout, CancellationToken.None);
            unloadVerified = running.Success ? !running.Models.Any(IsSelectedModel) : null;
        }

        DeterministicContextBenchmarkArtifacts.WriteAll(directory, manifest, _configuration, samples, unloadRequested, unloadVerified);
        int failed = samples.Count(sample => sample.OutputKind == "generation-failed");
        return new MusicContextBenchmarkRunResult(
            failed == 0,
            failed == 0 ? "M9C.4 benchmark completed. Qualitative review is still required." : "M9C.4 benchmark completed with generation failures; inspect artifacts.",
            directory,
            attempted,
            samples.Count - failed,
            failed,
            unloadRequested,
            unloadVerified);
    }

    private async Task<DeterministicContextBenchmarkSample> GenerateAsync(
        DeterministicContextScenario scenario,
        ZoyaPersonaMemory? persona,
        int sampleNumber,
        CommentPrompt prompt,
        IList<string> recentAcceptedLinks,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        OllamaGenerationResult generation = await _generator.GenerateAsync(prompt, _configuration.MeasurementKeepAlive, _configuration.GenerationTimeout, cancellationToken);
        stopwatch.Stop();
        DeterministicLinkValidationResult validation = generation.Status == OllamaGenerationStatus.Success
            ? DeterministicContextLinkValidator.Validate(generation.Response, scenario, persona, recentAcceptedLinks)
            : new DeterministicLinkValidationResult(false, false, CommentValidator.FallbackSignal, 0, "Generation failed: " + (generation.ErrorMessage ?? generation.Status.ToString()));
        if (validation.IsValid && !validation.IsFallback)
        {
            recentAcceptedLinks.Add(validation.NormalizedText);
            while (recentAcceptedLinks.Count > 6)
            {
                recentAcceptedLinks.RemoveAt(0);
            }
        }

        DeterministicContextComposition composition = DeterministicContextComposer.Compose(scenario, validation);
        string outputKind = generation.Status != OllamaGenerationStatus.Success ? "generation-failed" : composition.OutputKind;
        return new DeterministicContextBenchmarkSample(
            scenario.Id,
            scenario.Condition,
            persona?.Id,
            sampleNumber,
            HashPrompt(prompt),
            generation.Response,
            composition.FinalText,
            outputKind,
            stopwatch.Elapsed.TotalMilliseconds,
            validation.Reason);
    }

    private bool IsSelectedModel(OllamaInstalledModel model)
        => string.Equals(model.Name, _configuration.Model, StringComparison.OrdinalIgnoreCase)
            || string.Equals(model.Model, _configuration.Model, StringComparison.OrdinalIgnoreCase);

    private bool IsSelectedModel(OllamaRunningModel model)
        => string.Equals(model.Name, _configuration.Model, StringComparison.OrdinalIgnoreCase)
            || string.Equals(model.Model, _configuration.Model, StringComparison.OrdinalIgnoreCase);

    private static string HashPrompt(CommentPrompt prompt)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(prompt.SystemInstruction + "\n\0\n" + prompt.MetadataPrompt);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}

internal static class DeterministicContextBenchmarkArtifacts
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static void WriteAll(
        string directory,
        DeterministicContextBenchmarkManifest manifest,
        AiCommentConfiguration configuration,
        IReadOnlyList<DeterministicContextBenchmarkSample> samples,
        bool unloadRequested,
        bool? unloadVerified)
    {
        File.WriteAllText(Path.Combine(directory, "M9C4_RESULTS.json"), JsonSerializer.Serialize(new
        {
            manifestVersion = manifest.ManifestVersion,
            model = configuration.Model,
            generationTimeoutSeconds = configuration.GenerationTimeout.TotalSeconds,
            keepAlive = configuration.MeasurementKeepAlive,
            samples,
        }, JsonOptions), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(directory, "M9C4_EVALUATION.md"), BuildEvaluation(manifest, samples), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(directory, "M9C4_RUN_SUMMARY.md"), $"# M9C.4 run summary\n\n- Conditions: {manifest.Scenarios.Count}\n- Samples: {samples.Count}\n- Selected model: {configuration.Model}\n- Selected-model unload requested: {unloadRequested}\n- Selected-model unload verified: {unloadVerified?.ToString() ?? "not observable"}\n- This is mechanical evidence only; it does not pass M9C.\n", new UTF8Encoding(false));
    }

    private static string BuildEvaluation(DeterministicContextBenchmarkManifest manifest, IReadOnlyList<DeterministicContextBenchmarkSample> samples)
    {
        var builder = new StringBuilder("# Milestone 9C.4 deterministic-anchor evaluation\n\nThis sheet evaluates enriched creative quality only. Useful cards should normally produce a deterministic anchor plus a bounded host reaction; a weak card or unsafe reaction may safely produce FALLBACK. Bare anchor-only trivia is intentionally not a successful composition. The engineering refusal, timeout, cache, injection, Unicode, isolation, and unload checks remain automated-only.\n");
        foreach (DeterministicContextScenario scenario in manifest.Scenarios)
        {
            ZoyaPersonaMemory? persona = scenario.PersonaMemoryId is null ? null : manifest.PersonaMemories.Single(memory => memory.Id == scenario.PersonaMemoryId);
            builder.AppendLine($"\n## {scenario.Id}: {scenario.Description}");
            builder.AppendLine($"- Exact title: `{scenario.Title.Replace("`", "'")}`");
            builder.AppendLine($"- Exact artist: `{scenario.Artist.Replace("`", "'")}`");
            builder.AppendLine($"- Condition: `{scenario.Condition}`; hook kind: `{scenario.HookKind}`; editorial value: `{scenario.EditorialValue}`");
            builder.AppendLine($"- Deterministic anchor: {scenario.AnchorText}");
            builder.AppendLine($"- Source-backed hook: {scenario.HookDetail}");
            builder.AppendLine($"- Source: [{scenario.SourceName}]({scenario.SourceUrl})");
            builder.AppendLine($"- Selected persona memory: {(persona is null ? "`none`" : $"`{persona.Id}` — {persona.CanonicalText} ({persona.Classification})")}");
            foreach (DeterministicContextBenchmarkSample sample in samples.Where(sample => sample.ScenarioId == scenario.Id).OrderBy(sample => sample.SampleNumber))
            {
                builder.AppendLine($"\n### Sample {sample.SampleNumber}");
                builder.AppendLine($"\n> {sample.FinalComment}");
                builder.AppendLine($"\nComposition: `{sample.OutputKind}`. Mechanical result: `{sample.ValidatorOutcome}`");
                builder.AppendLine("\n| Factual grounding | Underlying hook usefulness | Anchor-to-link flow | Zoya personality | Persona coherence | Natural spoken phrasing | Variety | Overall: Would I want Zoya Radio to say this? | Notes |\n| --- | --- | --- | --- | --- | --- | --- | --- |\n|  |  |  |  |  |  |  |  |  |");
                builder.AppendLine("\n- [ ] Unsupported factual claim\n- [ ] Invented Zoya biography\n- [ ] Conflict with persona memory\n- [ ] Anchor recitation\n- [ ] Awkward deterministic/generated join\n- [ ] Repetitive structure\n- [ ] Generic DJ cliche\n- [ ] Unnecessary comment / should be FALLBACK\n- [ ] Unicode corruption/substitution");
            }
        }

        return builder.ToString();
    }
}
