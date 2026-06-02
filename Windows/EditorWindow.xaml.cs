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
    private readonly BitmapSource _originalImage;
    private Button? _selectedColorButton;

    public EditorWindow(BitmapSource image)
    {
        InitializeComponent();
        _originalImage = image;
        CaptureImage.Source = image;
        CaptureImage.Width = image.PixelWidth;
        CaptureImage.Height = image.PixelHeight;
        InkLayer.Width = image.PixelWidth;
        InkLayer.Height = image.PixelHeight;
        SelectColorButton(RedSwatch);
        UpdateDrawingAttributes();
    }

    private void PenButton_Click(object sender, RoutedEventArgs e)
    {
        InkLayer.EditingMode = InkCanvasEditingMode.Ink;
    }

    private void EraserButton_Click(object sender, RoutedEventArgs e)
    {
        InkLayer.EditingMode = InkCanvasEditingMode.EraseByStroke;
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

    private BitmapSource RenderEditedImage()
    {
        var width = _originalImage.PixelWidth;
        var height = _originalImage.PixelHeight;
        var target = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

        var surface = new Grid
        {
            Width = width,
            Height = height,
            Background = Brushes.Transparent
        };
        surface.Children.Add(new Image
        {
            Source = _originalImage,
            Width = width,
            Height = height,
            Stretch = Stretch.Fill
        });
        surface.Children.Add(new InkCanvas
        {
            Width = width,
            Height = height,
            Background = Brushes.Transparent,
            Strokes = new StrokeCollection(InkLayer.Strokes)
        });

        surface.Measure(new Size(width, height));
        surface.Arrange(new Rect(0, 0, width, height));
        surface.UpdateLayout();

        target.Render(surface);
        target.Freeze();
        return target;
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
            e.Handled = true;
        }
        else if (e.Key == Key.E)
        {
            InkLayer.EditingMode = InkCanvasEditingMode.EraseByStroke;
            e.Handled = true;
        }
    }
}
