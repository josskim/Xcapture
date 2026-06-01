using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace XCapture.Services;

public sealed class HotKeyManager : IDisposable
{
    private const int RegionCaptureId = 9001;
    private const int FullScreenCaptureId = 9002;
    private const int WmHotKey = 0x0312;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint VkS = 0x53;
    private const uint VkA = 0x41;

    private HwndSource? _source;
    private Window? _helperWindow;

    public event Action? RegionCapturePressed;
    public event Action? FullScreenCapturePressed;

    public void Register()
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

        RegisterHotKey(helper.Handle, RegionCaptureId, ModControl | ModShift, VkS);
        RegisterHotKey(helper.Handle, FullScreenCaptureId, ModControl | ModShift, VkA);
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
            UnregisterHotKey(_source.Handle, RegionCaptureId);
            UnregisterHotKey(_source.Handle, FullScreenCaptureId);
            _source.RemoveHook(WndProc);
            _source = null;
        }

        _helperWindow?.Close();
        _helperWindow = null;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
