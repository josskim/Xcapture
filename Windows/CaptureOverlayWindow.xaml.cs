using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using XCapture.Services;
using Canvas = System.Windows.Controls.Canvas;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace XCapture.Windows;

public partial class CaptureOverlayWindow : Window
{
    private const double CursorClearance = 24;
    private const int CaptureSettleDelayMilliseconds = 220;

    private Point _startPoint;
    private bool _isDragging;

    public BitmapSource? CapturedBitmap { get; private set; }
    public bool IsCaptureAccepted { get; private set; }

    public CaptureOverlayWindow()
    {
        InitializeComponent();
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
            Dispatcher.BeginInvoke(() =>
            {
                Activate();
                Focus();
                Keyboard.Focus(this);
            }, DispatcherPriority.ApplicationIdle);
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

        MoveCursorOutsideSelection(new Rect(x, y, width, height));
        Hide();
        Thread.Sleep(CaptureSettleDelayMilliseconds);
        CapturedBitmap = ScreenCaptureService.CaptureWpfRect(new Rect(Left + x, Top + y, width, height));
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

    private void MoveCursorOutsideSelection(Rect selection)
    {
        var centerX = selection.Left + selection.Width / 2;
        var centerY = selection.Top + selection.Height / 2;
        var candidates = new[]
        {
            new Point(selection.Left - CursorClearance, centerY),
            new Point(selection.Right + CursorClearance, centerY),
            new Point(centerX, selection.Top - CursorClearance),
            new Point(centerX, selection.Bottom + CursorClearance),
            new Point(CursorClearance, CursorClearance)
        };

        Point? target = null;
        foreach (var candidate in candidates)
        {
            if (IsInsideOverlay(candidate) && !selection.Contains(candidate))
            {
                target = candidate;
                break;
            }
        }

        if (target is null)
        {
            return;
        }

        var screenPoint = PointToScreen(target.Value);
        System.Windows.Forms.Cursor.Position = new System.Drawing.Point(
            (int)Math.Round(screenPoint.X),
            (int)Math.Round(screenPoint.Y));
    }

    private bool IsInsideOverlay(Point point)
    {
        return point.X >= 0 &&
               point.Y >= 0 &&
               point.X <= ActualWidth &&
               point.Y <= ActualHeight;
    }
}
