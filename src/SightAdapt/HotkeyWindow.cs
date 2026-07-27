namespace SightAdapt;

internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    private const int LocalToggleHotkeyId = 1;
    private const int ProfileToggleHotkeyId = 2;

    private readonly Action<int> _handler;
    private readonly INativeWindowApi _windowApi;
    private readonly HashSet<int> _registeredIds = [];
    private bool _disposed;

    public HotkeyWindow(Action<int> handler)
        : this(handler, NativeWindowApi.Default)
    {
    }

    internal HotkeyWindow(
        Action<int> handler,
        INativeWindowApi windowApi)
    {
        _handler = handler ??
            throw new ArgumentNullException(nameof(handler));
        _windowApi = windowApi ??
            throw new ArgumentNullException(nameof(windowApi));

        CreateHandle(new CreateParams
        {
            Caption = "SightAdapt Hotkey Window",
            Parent = NativeConstants.HwndMessage,
        });

        LocalToggleShortcut = RegisterExact(
            LocalToggleHotkeyId,
            NativeConstants.ModControl |
                NativeConstants.ModAlt |
                NativeConstants.ModNoRepeat,
            (uint)Keys.I,
            "Ctrl+Alt+I");

        ProfileToggleShortcut = RegisterExact(
            ProfileToggleHotkeyId,
            NativeConstants.ModControl |
                NativeConstants.ModAlt |
                NativeConstants.ModShift |
                NativeConstants.ModNoRepeat,
            (uint)Keys.I,
            "Ctrl+Alt+Shift+I");
    }

    public string? LocalToggleShortcut { get; }

    public string? ProfileToggleShortcut { get; }

    public static int LocalToggleId => LocalToggleHotkeyId;

    public static int ProfileToggleId => ProfileToggleHotkeyId;

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == NativeConstants.WmHotkey)
        {
            _handler(message.WParam.ToInt32());
            return;
        }

        base.WndProc(ref message);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var id in _registeredIds)
        {
            NativeCall.BestEffort(
                _windowApi.UnregisterHotKey(Handle, id),
                $"Unregister global hotkey {id}");
        }

        _registeredIds.Clear();
        DestroyHandle();
        _disposed = true;
    }

    private string? RegisterExact(
        int id,
        uint modifiers,
        uint key,
        string shortcutText)
    {
        if (!_windowApi.RegisterHotKey(
                Handle,
                id,
                modifiers,
                key))
        {
            Diagnostics.Report(
                nameof(HotkeyWindow),
                "Register global hotkey",
                DiagnosticSeverity.Warning,
                DiagnosticFailurePolicy.Recovered,
                $"The shortcut {shortcutText} is unavailable.");
            return null;
        }

        _registeredIds.Add(id);
        return shortcutText;
    }
}
