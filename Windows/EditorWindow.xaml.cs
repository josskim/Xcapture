using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using XCapture.Services;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Image = System.Windows.Controls.Image;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using PrintDialog = System.Windows.Controls.PrintDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Size = System.Windows.Size;

namespace XCapture.Windows;

public partial class EditorWindow : Window
{
    private const double MinZoom = 0.1;
    private const double MaxZoom = 4.0;
    private const double ZoomStep = 1.2;

    private BitmapSource _originalImage;
    private readonly Action _showMainWindow;
    private Button? _selectedColorButton;
    private Button? _selectedToolButton;
    private bool _fitToWindow = true;
    private double _zoom = 1.0;
    private double _displayWidth;
    private double _displayHeight;

    public EditorWindow(BitmapSource image, Action showMainWindow)
    {
        InitializeComponent();
        _originalImage = image;
        _showMainWindow = showMainWindow;
        SetImage(image, clearStrokes: true);
        SelectColorButton(RedSwatch);
        SelectToolButton(PenButton);
        InkLayer.EditingMode = InkCanvasEditingMode.Ink;
        UpdateDrawingAttributes();
    }

    public void ReplaceImage(BitmapSource image)
    {
        _originalImage = image;
        SetImage(image, clearStrokes: true);
        _fitToWindow = true;
        FitImageToWindow();
    }

    private void SetImage(BitmapSource image, bool clearStrokes)
    {
        CaptureImage.Source = image;
        UpdateImageDisplaySize();

        if (clearStrokes)
        {
            InkLayer.Strokes.Clear();
        }
    }

    private void PenButton_Click(object sender, RoutedEventArgs e)
    {
        InkLayer.EditingMode = InkCanvasEditingMode.Ink;
        SelectToolButton(PenButton);
    }

    private void EraserButton_Click(object sender, RoutedEventArgs e)
    {
        InkLayer.EditingMode = InkCanvasEditingMode.EraseByStroke;
        SelectToolButton(EraserButton);
    }

    private void ColorSwatch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            SelectColorButton(button);
        }

        UpdateDrawingAttributes();
    }

    private void StrokeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateDrawingAttributes();
    }

    private void UpdateDrawingAttributes()
    {
        if (InkLayer is null || _selectedColorButton?.Tag is not string colorValue) return;

        var color = (Color)ColorConverter.ConvertFromString(colorValue);
        InkLayer.DefaultDrawingAttributes = new DrawingAttributes
        {
            Color = color,
            Width = StrokeSlider?.Value ?? 4,
            Height = StrokeSlider?.Value ?? 4,
            FitToCurve = true,
            IgnorePressure = true
        };
    }

    private void SelectColorButton(Button button)
    {
        if (_selectedColorButton is not null)
        {
            _selectedColorButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D5DB"));
            _selectedColorButton.BorderThickness = new Thickness(1);
        }

        _selectedColorButton = button;
        _selectedColorButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111827"));
        _selectedColorButton.BorderThickness = new Thickness(3);
    }

    private void SelectToolButton(Button button)
    {
        if (_selectedToolButton is not null)
        {
            _selectedToolButton.Background = Brushes.White;
            _selectedToolButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111827"));
            _selectedToolButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D5DB"));
        }

        _selectedToolButton = button;
        _selectedToolButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111827"));
        _selectedToolButton.Foreground = Brushes.White;
        _selectedToolButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111827"));
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        ClipboardService.TryCopyImage(RenderEditedImage());
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PNG 이미지 (*.png)|*.png",
            FileName = $"xcapture-{DateTime.Now:yyyyMMdd-HHmmss}.png"
        };

        if (dialog.ShowDialog() == true)
        {
            FileService.SavePng(RenderEditedImage(), dialog.FileName);
        }
    }

    private void PrintButton_Click(object sender, RoutedEventArgs e)
    {
        var image = new Image
        {
            Source = RenderEditedImage(),
            Stretch = Stretch.Uniform
        };

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() == true)
        {
            image.Width = dialog.PrintableAreaWidth;
            image.Height = dialog.PrintableAreaHeight;
            dialog.PrintVisual(image, "XCapture");
        }
    }

    private void UndoButton_Click(object sender, RoutedEventArgs e)
    {
        if (InkLayer.Strokes.Count > 0)
        {
            InkLayer.Strokes.RemoveAt(InkLayer.Strokes.Count - 1);
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        InkLayer.Strokes.Clear();
    }

    private void CaptureListButton_Click(object sender, RoutedEventArgs e)
    {
        _showMainWindow();
    }

    private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
    {
        _fitToWindow = false;
        SetZoom(GetSteppedZoom(zoomIn: false));
    }

    private void ZoomFitButton_Click(object sender, RoutedEventArgs e)
    {
        _fitToWindow = true;
        FitImageToWindow();
    }

    private void ZoomInButton_Click(object sender, RoutedEventArgs e)
    {
        _fitToWindow = false;
        SetZoom(GetSteppedZoom(zoomIn: true));
    }

    private void ImageScrollViewer_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateImageDisplaySize();
        FitImageToWindow();
    }

    private void ImageScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_fitToWindow)
        {
            FitImageToWindow();
        }
    }

    private void FitImageToWindow()
    {
        if (_originalImage.PixelWidth <= 0 || _originalImage.PixelHeight <= 0)
        {
            return;
        }

        var availableWidth = Math.Max(1, ImageScrollViewer.ViewportWidth - 56);
        var availableHeight = Math.Max(1, ImageScrollViewer.ViewportHeight - 56);
        if (double.IsNaN(availableWidth) || double.IsInfinity(availableWidth) || availableWidth <= 1)
        {
            availableWidth = Math.Max(1, ImageScrollViewer.ActualWidth - 56);
        }

        if (double.IsNaN(availableHeight) || double.IsInfinity(availableHeight) || availableHeight <= 1)
        {
            availableHeight = Math.Max(1, ImageScrollViewer.ActualHeight - 56);
        }

        var widthScale = availableWidth / Math.Max(1, _displayWidth);
        var heightScale = availableHeight / Math.Max(1, _displayHeight);
        SetZoom(Math.Min(1.0, Math.Min(MaxZoom, Math.Min(widthScale, heightScale))));
    }

    private void SetZoom(double zoom)
    {
        _zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
        CaptureScale.ScaleX = _zoom;
        CaptureScale.ScaleY = _zoom;
        ZoomText.Text = $"{Math.Round(_zoom * 100)}%";
    }

    private double GetSteppedZoom(bool zoomIn)
    {
        var nextZoom = zoomIn ? _zoom * ZoomStep : _zoom / ZoomStep;
        if (zoomIn && _zoom < 1.0 && nextZoom > 1.0)
        {
            return 1.0;
        }

        if (!zoomIn && _zoom > 1.0 && nextZoom < 1.0)
        {
            return 1.0;
        }

        return nextZoom;
    }

    private BitmapSource RenderEditedImage()
    {
        var width = _originalImage.PixelWidth;
        var height = _originalImage.PixelHeight;
        var dpi = GetCurrentDpi();
        var target = new RenderTargetBitmap(width, height, 96 * dpi.DpiScaleX, 96 * dpi.DpiScaleY, PixelFormats.Pbgra32);
        var displayWidth = Math.Max(1, width / dpi.DpiScaleX);
        var displayHeight = Math.Max(1, height / dpi.DpiScaleY);

        var surface = new Grid
        {
            Width = displayWidth,
            Height = displayHeight,
            Background = Brushes.Transparent
        };
        surface.Children.Add(new Image
        {
            Source = _originalImage,
            Width = displayWidth,
            Height = displayHeight,
            Stretch = Stretch.Fill
        });
        surface.Children.Add(new InkCanvas
        {
            Width = displayWidth,
            Height = displayHeight,
            Background = Brushes.Transparent,
            Strokes = new StrokeCollection(InkLayer.Strokes)
        });

        surface.Measure(new Size(displayWidth, displayHeight));
        surface.Arrange(new Rect(0, 0, displayWidth, displayHeight));
        surface.UpdateLayout();

        target.Render(surface);
        target.Freeze();
        return target;
    }

    private void UpdateImageDisplaySize()
    {
        var dpi = GetCurrentDpi();
        _displayWidth = Math.Max(1, _originalImage.PixelWidth / dpi.DpiScaleX);
        _displayHeight = Math.Max(1, _originalImage.PixelHeight / dpi.DpiScaleY);

        CaptureSurface.Width = _displayWidth;
        CaptureSurface.Height = _displayHeight;
        CaptureImage.Width = _displayWidth;
        CaptureImage.Height = _displayHeight;
        InkLayer.Width = _displayWidth;
        InkLayer.Height = _displayHeight;
    }

    private DpiScale GetCurrentDpi()
    {
        if (PresentationSource.FromVisual(this) is null)
        {
            return new DpiScale(1.0, 1.0);
        }

        return VisualTreeHelper.GetDpi(this);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
        {
            ClipboardService.TryCopyImage(RenderEditedImage());
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
        {
            SaveButton_Click(sender, e);
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.P)
        {
            PrintButton_Click(sender, e);
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Z)
        {
            UndoButton_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.P)
        {
            InkLayer.EditingMode = InkCanvasEditingMode.Ink;
            SelectToolButton(PenButton);
            e.Handled = true;
        }
        else if (e.Key == Key.E)
        {
            InkLayer.EditingMode = InkCanvasEditingMode.EraseByStroke;
            SelectToolButton(EraserButton);
            e.Handled = true;
        }
    }
}
