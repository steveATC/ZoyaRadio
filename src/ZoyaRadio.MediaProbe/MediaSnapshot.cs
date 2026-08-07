namespace ZoyaRadio.MediaProbe;

internal sealed record MediaSnapshot(string? Title, string? Artist, string? Album)
{
    public static MediaSnapshot Empty { get; } = new(null, null, null);

    public static MediaSnapshot FromWindows(string? title, string? artist, string? album)
    {
        return new MediaSnapshot(Normalize(title), Normalize(artist), Normalize(album));
    }

    public static string Display(string? value)
    {
        return value ?? "<not provided>";
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
