namespace SightAdapt;

internal enum ApplicationRunState
{
    Inactive,
    ManualActive,
    AutomaticActive,
    Fault,
    Emergency,
}

internal sealed record ApplicationState(
    ApplicationRunState Kind,
    nint TargetWindow = default,
    string? VisualProfileId = null,
    nint AutomaticSuppressedWindow = default,
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
        Current.Kind is not ApplicationRunState.Emergency and
        not ApplicationRunState.Fault;

    public event EventHandler<ApplicationStateChangedEventArgs>? Changed;

    public void SetInactive()
    {
        TransitionTo(new ApplicationState(
            ApplicationRunState.Inactive,
            AutomaticSuppressedWindow: Current.AutomaticSuppressedWindow));
    }

    public void SetManualActive(nint targetWindow, string visualProfileId)
    {
        RequireTarget(targetWindow);
        RequireVisualProfile(visualProfileId);

        TransitionTo(new ApplicationState(
            ApplicationRunState.ManualActive,
            targetWindow,
            visualProfileId,
            Current.AutomaticSuppressedWindow));
    }

    public void SetAutomaticActive(nint targetWindow, string visualProfileId)
    {
        RequireTarget(targetWindow);
        RequireVisualProfile(visualProfileId);

        if (!AllowsAutomaticActivation)
        {
            throw new InvalidOperationException(
                "Automatic activation is blocked while a fault or emergency state is active.");
        }

        TransitionTo(new ApplicationState(
            ApplicationRunState.AutomaticActive,
            targetWindow,
            visualProfileId,
            Current.AutomaticSuppressedWindow));
    }

    public void SetFault(
        string message,
        nint automaticSuppressedWindow = default)
    {
        TransitionTo(new ApplicationState(
            ApplicationRunState.Fault,
            AutomaticSuppressedWindow:
                automaticSuppressedWindow != nint.Zero
                    ? automaticSuppressedWindow
                    : Current.AutomaticSuppressedWindow,
            Message: NormalizeMessage(
                message,
                "The visual correction could not be applied.")));
    }

    public void SetEmergency(string message)
    {
        TransitionTo(new ApplicationState(
            ApplicationRunState.Emergency,
            Message: NormalizeMessage(message, "All overlays stopped.")));
    }

    public void SuppressAutomaticFor(nint targetWindow)
    {
        RequireTarget(targetWindow);
        TransitionTo(Current with { AutomaticSuppressedWindow = targetWindow });
    }

    public void ClearAutomaticSuppression()
    {
        if (Current.AutomaticSuppressedWindow == nint.Zero)
        {
            return;
        }

        TransitionTo(Current with { AutomaticSuppressedWindow = nint.Zero });
    }

    public void ObserveForeground(nint targetWindow)
    {
        if (Current.AutomaticSuppressedWindow != nint.Zero &&
            Current.AutomaticSuppressedWindow != targetWindow)
        {
            ClearAutomaticSuppression();
        }
    }

    public bool IsAutomaticSuppressedFor(nint targetWindow)
    {
        return targetWindow != nint.Zero &&
            Current.AutomaticSuppressedWindow == targetWindow;
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

    private static string NormalizeMessage(string message, string fallback)
    {
        return string.IsNullOrWhiteSpace(message) ? fallback : message.Trim();
    }

    private static void RequireTarget(nint targetWindow)
    {
        if (targetWindow == nint.Zero)
        {
            throw new ArgumentException(
                "An active target window is required.",
                nameof(targetWindow));
        }
    }

    private static void RequireVisualProfile(string visualProfileId)
    {
        if (string.IsNullOrWhiteSpace(visualProfileId))
        {
            throw new ArgumentException(
                "An active visual profile identifier is required.",
                nameof(visualProfileId));
        }
    }
}
