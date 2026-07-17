namespace SightAdapt.Demo;

internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    private const int ToggleHotkeyId = 1;
    private const int EmergencyHotkeyId = 2;

    private readonly Action<int> _handler;
    private readonly HashSet<int> _registeredIds = [];
    private bool _disposed;

    public HotkeyWindow(Action<int> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));

        CreateHandle(new CreateParams
        {
            Caption = "SightAdapt Hotkey Window",
            Parent = NativeMethods.HwndMessage,
        });

        ToggleShortcut = RegisterWithFallback(
            ToggleHotkeyId,
            NativeMethods.ModControl | NativeMethods.ModWin | NativeMethods.ModNoRepeat,
            (uint)Keys.D2,
            NativeMethods.ModControl | NativeMethods.ModAlt | NativeMethods.ModNoRepeat,
            (uint)Keys.I,
            "Ctrl+Win+2",
            "Ctrl+Alt+I");

        EmergencyShortcut = RegisterWithFallback(
            EmergencyHotkeyId,
            NativeMethods.ModControl | NativeMethods.ModWin | NativeMethods.ModShift | NativeMethods.ModNoRepeat,
            (uint)Keys.D2,
            NativeMethods.ModControl | NativeMethods.ModAlt | NativeMethods.ModShift | NativeMethods.ModNoRepeat,
            (uint)Keys.I,
            "Ctrl+Win+Shift+2",
            "Ctrl+Alt+Shift+I");
    }

    public string? ToggleShortcut { get; }

    public string? EmergencyShortcut { get; }

    public static int ToggleId => ToggleHotkeyId;

    public static int EmergencyId => EmergencyHotkeyId;

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == NativeMethods.WmHotkey)
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
            NativeMethods.UnregisterHotKey(Handle, id);
        }

        _registeredIds.Clear();
        DestroyHandle();
        _disposed = true;
    }

    private string? RegisterWithFallback(
        int id,
        uint preferredModifiers,
        uint preferredKey,
        uint fallbackModifiers,
        uint fallbackKey,
        string preferredText,
        string fallbackText)
    {
        if (NativeMethods.RegisterHotKey(Handle, id, preferredModifiers, preferredKey))
        {
            _registeredIds.Add(id);
            return preferredText;
        }

        if (NativeMethods.RegisterHotKey(Handle, id, fallbackModifiers, fallbackKey))
        {
            _registeredIds.Add(id);
            return fallbackText;
        }

        return null;
    }
}
