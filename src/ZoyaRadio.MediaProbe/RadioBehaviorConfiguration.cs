namespace ZoyaRadio.MediaProbe;

internal sealed record RadioBehaviorConfiguration(
    int MinimumTransitionsBetweenComments,
    int MaximumTransitionsBetweenComments,
    TimeSpan MinimumTrackAgeBeforeSpeech,
    TimeSpan MaximumSpeechDuration,
    int RecentTrackLimit,
    int RecentCommentLimit)
{
    public static RadioBehaviorConfiguration Default { get; } = new(
        MinimumTransitionsBetweenComments: 2,
        MaximumTransitionsBetweenComments: 5,
        MinimumTrackAgeBeforeSpeech: TimeSpan.FromSeconds(8),
        MaximumSpeechDuration: TimeSpan.FromSeconds(25),
        RecentTrackLimit: 10,
        RecentCommentLimit: 6);

    public void Validate()
    {
        if (MinimumTransitionsBetweenComments is < 1 or > 50)
        {
            throw new InvalidOperationException(
                "MinimumTransitionsBetweenComments must be between 1 and 50.");
        }

        if (MaximumTransitionsBetweenComments < MinimumTransitionsBetweenComments
            || MaximumTransitionsBetweenComments > 50)
        {
            throw new InvalidOperationException(
                "MaximumTransitionsBetweenComments must be at least the minimum and no greater than 50.");
        }

        if (MinimumTrackAgeBeforeSpeech <= TimeSpan.Zero
            || MinimumTrackAgeBeforeSpeech > TimeSpan.FromMinutes(5))
        {
            throw new InvalidOperationException(
                "MinimumTrackAgeBeforeSpeech must be greater than zero and no more than 5 minutes.");
        }

        if (MaximumSpeechDuration <= TimeSpan.Zero
            || MaximumSpeechDuration > TimeSpan.FromMinutes(2))
        {
            throw new InvalidOperationException(
                "MaximumSpeechDuration must be greater than zero and no more than 2 minutes.");
        }

        if (RecentTrackLimit is < 1 or > 100)
        {
            throw new InvalidOperationException("RecentTrackLimit must be between 1 and 100.");
        }

        if (RecentCommentLimit is < 1 or > 50)
        {
            throw new InvalidOperationException("RecentCommentLimit must be between 1 and 50.");
        }
    }
}

internal interface ITransitionTargetSource
{
    int NextInclusive(int minimum, int maximum);
}

internal sealed class RandomTransitionTargetSource : ITransitionTargetSource
{
    public int NextInclusive(int minimum, int maximum)
    {
        return Random.Shared.Next(minimum, checked(maximum + 1));
    }
}
