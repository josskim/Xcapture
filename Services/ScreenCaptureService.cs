using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace XCapture.Services;

public static class ScreenCaptureService
{
    public static BitmapSource CaptureAllScreens()
    {
        var bounds = System.Windows.Forms.Screen.AllScreens
            .Select(screen => screen.Bounds)
            .Aggregate(Rectangle.Union);

        return CaptureRectangle(bounds);
    }

    public static BitmapSource CaptureWpfRect(Rect rect)
    {
        var dpiScale = GetPrimaryDpiScale();
        var bounds = new Rectangle(
            (int)Math.Round(rect.X * dpiScale),
            (int)Math.Round(rect.Y * dpiScale),
            (int)Math.Round(rect.Width * dpiScale),
            (int)Math.Round(rect.Height * dpiScale));

        return CaptureRectangle(bounds);
    }

    private static BitmapSource CaptureRectangle(Rectangle bounds)
    {
        using var bitmap = new Bitmap(bounds.Width, bounds.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Location, System.Drawing.Point.Empty, bounds.Size);
        var handle = bitmap.GetHbitmap();
        try
        {
            var source = Imaging.CreateBitmapSourceFromHBitmap(
                handle,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            DeleteObject(handle);
        }
    }

    private static double GetPrimaryDpiScale()
    {
        var main = System.Windows.Application.Current.MainWindow;
        if (main is null) return 1.0;
        var source = PresentationSource.FromVisual(main);
        return source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
    }

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);
}
