using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Canvas = System.Windows.Controls.Canvas;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace XCapture.Windows;

public partial class CaptureOverlayWindow : Window
{
    private readonly BitmapSource _snapshot;
    private Point _startPoint;
    private bool _isDragging;

    public BitmapSource? CapturedBitmap { get; private set; }
    public bool IsCaptureAccepted { get; private set; }

    public CaptureOverlayWindow(BitmapSource snapshot)
    {
        _snapshot = snapshot;
        InitializeComponent();
        SnapshotImage.Source = snapshot;

        Loaded += (_, _) =>
        {
            var bounds = System.Windows.Forms.Screen.AllScreens
                .Select(screen => screen.Bounds)
                .Aggregate(System.Drawing.Rectangle.Union);

            Left = bounds.Left;
            Top = bounds.Top;
            Width = bounds.Width;
            Height = bounds.Height;
            Activate();
            Focus();
            Keyboard.Focus(this);
        };
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Right)
        {
            CancelCapture();
            return;
        }

        _isDragging = true;
        _startPoint = e.GetPosition(this);
        SelectionRect.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionRect, _startPoint.X);
        Canvas.SetTop(SelectionRect, _startPoint.Y);
        SelectionRect.Width = 0;
        SelectionRect.Height = 0;
        CaptureMouse();
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;

        var current = e.GetPosition(this);
        var x = Math.Min(current.X, _startPoint.X);
        var y = Math.Min(current.Y, _startPoint.Y);
        var width = Math.Abs(current.X - _startPoint.X);
        var height = Math.Abs(current.Y - _startPoint.Y);

        Canvas.SetLeft(SelectionRect, x);
        Canvas.SetTop(SelectionRect, y);
        SelectionRect.Width = width;
        SelectionRect.Height = height;
    }

    private void Window_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;

        _isDragging = false;
        ReleaseMouseCapture();

        var x = Canvas.GetLeft(SelectionRect);
        var y = Canvas.GetTop(SelectionRect);
        var width = SelectionRect.Width;
        var height = SelectionRect.Height;

        if (width < 8 || height < 8)
        {
            CancelCapture();
            return;
        }

        var scaleX = _snapshot.PixelWidth / ActualWidth;
        var scaleY = _snapshot.PixelHeight / ActualHeight;
        var crop = new Int32Rect(
            Math.Clamp((int)Math.Round(x * scaleX), 0, _snapshot.PixelWidth - 1),
            Math.Clamp((int)Math.Round(y * scaleY), 0, _snapshot.PixelHeight - 1),
            Math.Clamp((int)Math.Round(width * scaleX), 1, _snapshot.PixelWidth),
            Math.Clamp((int)Math.Round(height * scaleY), 1, _snapshot.PixelHeight));

        crop.Width = Math.Min(crop.Width, _snapshot.PixelWidth - crop.X);
        crop.Height = Math.Min(crop.Height, _snapshot.PixelHeight - crop.Y);

        CapturedBitmap = new CroppedBitmap(_snapshot, crop);
        CapturedBitmap.Freeze();
        IsCaptureAccepted = true;
        Close();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CancelCapture();
            e.Handled = true;
        }
    }

    private void CancelCapture()
    {
        _isDragging = false;
        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }

        CapturedBitmap = null;
        IsCaptureAccepted = false;
        Close();
    }
}
