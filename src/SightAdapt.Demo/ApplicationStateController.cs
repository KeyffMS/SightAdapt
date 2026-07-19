namespace SightAdapt.Demo;

internal enum ApplicationRunState
{
    Inactive,
    ManualActive,
    AutomaticActive,
    Emergency,
}

internal sealed record ApplicationState(
    ApplicationRunState Kind,
    nint TargetWindow = default,
    string? Message = null)
{
    public bool HasActiveOverlay =>
        Kind is ApplicationRunState.ManualActive or ApplicationRunState.AutomaticActive;
}

internal sealed class ApplicationStateChangedEventArgs(
    ApplicationState previous,
    ApplicationState current) : EventArgs
{
    public ApplicationState Previous { get; } = previous;

    public ApplicationState Current { get; } = current;
}

internal sealed class ApplicationStateController
{
    public ApplicationState Current { get; private set; } =
        new(ApplicationRunState.Inactive);

    public bool AllowsAutomaticActivation =>
        Current.Kind != ApplicationRunState.Emergency;

    public event EventHandler<ApplicationStateChangedEventArgs>? Changed;

    public void SetInactive()
    {
        TransitionTo(new ApplicationState(ApplicationRunState.Inactive));
    }

    public void SetManualActive(nint targetWindow)
    {
        RequireTarget(targetWindow);
        TransitionTo(new ApplicationState(ApplicationRunState.ManualActive, targetWindow));
    }

    public void SetAutomaticActive(nint targetWindow)
    {
        RequireTarget(targetWindow);

        if (!AllowsAutomaticActivation)
        {
            throw new InvalidOperationException(
                "Automatic activation is blocked while emergency state is active.");
        }

        TransitionTo(new ApplicationState(ApplicationRunState.AutomaticActive, targetWindow));
    }

    public void SetEmergency(string message)
    {
        var normalizedMessage = string.IsNullOrWhiteSpace(message)
            ? "All overlays stopped."
            : message.Trim();

        TransitionTo(new ApplicationState(
            ApplicationRunState.Emergency,
            Message: normalizedMessage));
    }

    private void TransitionTo(ApplicationState next)
    {
        if (Current == next)
        {
            return;
        }

        var previous = Current;
        Current = next;
        Changed?.Invoke(this, new ApplicationStateChangedEventArgs(previous, next));
    }

    private static void RequireTarget(nint targetWindow)
    {
        if (targetWindow == nint.Zero)
        {
            throw new ArgumentException("An active target window is required.", nameof(targetWindow));
        }
    }
}
