namespace SightAdapt;

internal sealed record RuntimeTimingPolicy
{
    public static RuntimeTimingPolicy Default { get; } = new(
        overlayRefreshMilliseconds: 33,
        foregroundPollMilliseconds: 75,
        menuPollMilliseconds: 75,
        foregroundTransitionGraceMilliseconds: 125,
        faultRecoveryMilliseconds: 5000);

    public RuntimeTimingPolicy(
        int overlayRefreshMilliseconds,
        int foregroundPollMilliseconds,
        int menuPollMilliseconds,
        int foregroundTransitionGraceMilliseconds,
        int faultRecoveryMilliseconds)
    {
        OverlayRefreshMilliseconds = RequirePositive(
            overlayRefreshMilliseconds,
            nameof(overlayRefreshMilliseconds));
        ForegroundPollMilliseconds = RequirePositive(
            foregroundPollMilliseconds,
            nameof(foregroundPollMilliseconds));
        MenuPollMilliseconds = RequirePositive(
            menuPollMilliseconds,
            nameof(menuPollMilliseconds));
        ForegroundTransitionGraceMilliseconds = RequirePositive(
            foregroundTransitionGraceMilliseconds,
            nameof(foregroundTransitionGraceMilliseconds));
        FaultRecoveryMilliseconds = RequirePositive(
            faultRecoveryMilliseconds,
            nameof(faultRecoveryMilliseconds));

        if (ForegroundTransitionGraceMilliseconds <=
            ForegroundPollMilliseconds)
        {
            throw new ArgumentException(
                "Foreground transition grace must exceed the foreground polling interval.",
                nameof(foregroundTransitionGraceMilliseconds));
        }
    }

    public int OverlayRefreshMilliseconds { get; }

    public int ForegroundPollMilliseconds { get; }

    public int MenuPollMilliseconds { get; }

    public int ForegroundTransitionGraceMilliseconds { get; }

    public int FaultRecoveryMilliseconds { get; }

    private static int RequirePositive(
        int value,
        string parameterName)
    {
        return value > 0
            ? value
            : throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "A positive interval is required.");
    }
}
