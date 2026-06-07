using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using XCapture.Models;

namespace XCapture.Services;

public sealed class HotKeyManager : IDisposable
{
    private const int RegionCaptureId = 9001;
    private const int FullScreenCaptureId = 9002;
    private const int WmHotKey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWindows = 0x0008;

    private HwndSource? _source;
    private Window? _helperWindow;
    private AppSettings _settings = new();
    private bool _hasRegisteredSettings;

    public event Action? RegionCapturePressed;
    public event Action? FullScreenCapturePressed;

    public bool Register(AppSettings settings)
    {
        if (_helperWindow is null)
        {
            _helperWindow = new Window
            {
                Width = 0,
                Height = 0,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Opacity = 0
            };
            _helperWindow.Show();
            _helperWindow.Hide();

            var helper = new WindowInteropHelper(_helperWindow);
            _source = HwndSource.FromHwnd(helper.Handle);
            _source?.AddHook(WndProc);
        }

        return Apply(settings);
    }

    public bool Apply(AppSettings settings)
    {
        if (_source is null)
        {
            return false;
        }

        var previous = _settings;
        var hadPreviousRegistration = _hasRegisteredSettings;
        UnregisterCurrent();

        if (RegisterGesture(RegionCaptureId, settings.RegionCaptureHotKey) &&
            RegisterGesture(FullScreenCaptureId, settings.FullScreenCaptureHotKey))
        {
            _settings = settings;
            _hasRegisteredSettings = true;
            return true;
        }

        UnregisterCurrent();
        if (hadPreviousRegistration &&
            RegisterGesture(RegionCaptureId, previous.RegionCaptureHotKey) &&
            RegisterGesture(FullScreenCaptureId, previous.FullScreenCaptureHotKey))
        {
            _hasRegisteredSettings = true;
        }
        else
        {
            UnregisterCurrent();
            _hasRegisteredSettings = false;
        }

        return false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmHotKey) return IntPtr.Zero;

        var id = wParam.ToInt32();
        if (id == RegionCaptureId)
        {
            RegionCapturePressed?.Invoke();
            handled = true;
        }
        else if (id == FullScreenCaptureId)
        {
            FullScreenCapturePressed?.Invoke();
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_source is not null)
        {
            UnregisterCurrent();
            _hasRegisteredSettings = false;
            _source.RemoveHook(WndProc);
            _source = null;
        }

        _helperWindow?.Close();
        _helperWindow = null;
    }

    private bool RegisterGesture(int id, HotKeyGesture gesture)
    {
        return _source is not null &&
               RegisterHotKey(
                   _source.Handle,
                   id,
                   GetModifiers(gesture),
                   (uint)KeyInterop.VirtualKeyFromKey(gesture.Key));
    }

    private void UnregisterCurrent()
    {
        if (_source is null)
        {
            return;
        }

        UnregisterHotKey(_source.Handle, RegionCaptureId);
        UnregisterHotKey(_source.Handle, FullScreenCaptureId);
    }

    private static uint GetModifiers(HotKeyGesture gesture)
    {
        var modifiers = 0u;
        if (gesture.Control) modifiers |= ModControl;
        if (gesture.Shift) modifiers |= ModShift;
        if (gesture.Alt) modifiers |= ModAlt;
        if (gesture.Windows) modifiers |= ModWindows;
        return modifiers;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
