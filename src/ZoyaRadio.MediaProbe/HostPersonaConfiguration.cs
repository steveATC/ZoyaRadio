namespace ZoyaRadio.MediaProbe;

internal sealed record HostPersonaConfiguration(string HostName, string ListenerName)
{
    public static HostPersonaConfiguration Default { get; } = new("Zoya", "Steve");

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(HostName))
        {
            throw new InvalidOperationException("The host name is required.");
        }

        if (string.IsNullOrWhiteSpace(ListenerName))
        {
            throw new InvalidOperationException("The listener name is required.");
        }
    }
}
