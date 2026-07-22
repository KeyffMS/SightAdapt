namespace SightAdapt;

internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    private const int LocalToggleHotkeyId = 1;
    private const int ProfileToggleHotkeyId = 2;

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

        LocalToggleShortcut = RegisterExact(
            LocalToggleHotkeyId,
            NativeMethods.ModControl |
                NativeMethods.ModAlt |
                NativeMethods.ModNoRepeat,
            (uint)Keys.I,
            "Ctrl+Alt+I");

        ProfileToggleShortcut = RegisterExact(
            ProfileToggleHotkeyId,
            NativeMethods.ModControl |
                NativeMethods.ModAlt |
                NativeMethods.ModShift |
                NativeMethods.ModNoRepeat,
            (uint)Keys.I,
            "Ctrl+Alt+Shift+I");
    }

    public string? LocalToggleShortcut { get; }

    public string? ProfileToggleShortcut { get; }

    public static int LocalToggleId => LocalToggleHotkeyId;

    public static int ProfileToggleId => ProfileToggleHotkeyId;

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

    private string? RegisterExact(int id, uint modifiers, uint key, string shortcutText)
    {
        if (!NativeMethods.RegisterHotKey(Handle, id, modifiers, key))
        {
            return null;
        }

        _registeredIds.Add(id);
        return shortcutText;
    }
}
