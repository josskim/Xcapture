using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace XCapture.Services;

public static class ScrollCaptureTarget
{
    public static Rectangle Resolve(Rectangle selected)
    {
        var point = new NativePoint { X = selected.Left + selected.Width / 2, Y = selected.Top + selected.Height / 2 };
        var hit = WindowFromPoint(point);
        var root = GetAncestor(hit, 2);
        if (root == IntPtr.Zero) throw new InvalidOperationException("스크롤할 화면을 찾지 못했습니다.");
        var rootClass = ClassName(root);
        if (rootClass != "Chrome_WidgetWin_1")
            throw new InvalidOperationException("현재 스크롤 전체 캡처는 Chrome 또는 Edge 브라우저에서 지원합니다. 브라우저의 페이지 안쪽을 선택해주세요.");

        var candidates = new List<Rectangle>();
        EnumChildWindows(root, (window, _) =>
        {
            if (!IsWindowVisible(window) || ClassName(window) != "Chrome_RenderWidgetHostHWND" || !GetWindowRect(window, out var rect)) return true;
            var bounds = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
            if (bounds.Contains(point.X, point.Y) && bounds.Width > 100 && bounds.Height > 100) candidates.Add(bounds);
            return true;
        }, IntPtr.Zero);
        if (candidates.Count == 0) throw new InvalidOperationException("브라우저 페이지 영역을 찾지 못했습니다. 페이지 내용 위에서 다시 선택해주세요.");
        var viewport = candidates.OrderByDescending(rect => (long)rect.Width * rect.Height).First();
        var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(point.X, point.Y));
        viewport = Rectangle.Intersect(viewport, screen.Bounds);
        if (viewport.Width < 100 || viewport.Height < 100) throw new InvalidOperationException("브라우저 페이지 영역이 화면 밖에 있습니다.");
        SetForegroundWindow(root);
        return viewport;
    }

    private static string ClassName(IntPtr window)
    {
        var text = new StringBuilder(256);
        GetClassName(window, text, text.Capacity);
        return text.ToString();
    }
    private delegate bool EnumWindowsProc(IntPtr window, IntPtr param);
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X; public int Y; }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left; public int Top; public int Right; public int Bottom; }
    [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(NativePoint point);
    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr window, uint flags);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr parent, EnumWindowsProc callback, IntPtr param);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out NativeRect rect);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder text, int length);
}
