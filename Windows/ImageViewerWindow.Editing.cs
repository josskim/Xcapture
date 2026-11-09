using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using XCapture.Services;
using Button = System.Windows.Controls.Button;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using MessageBox = System.Windows.MessageBox;

namespace XCapture.Windows;

public partial class ImageViewerWindow
{
    private readonly Stack<StrokeCollection> _undo = new();
    private StrokeCollection _lastStrokes = new();
    private bool _changingStrokes;
    private bool _hasUnsavedEdits;
    private Color _penColor = Colors.Red;

    private void InitializeEditing()
    {
        InkLayer.Strokes.StrokesChanged += StrokesChanged;
        UpdateDrawingAttributes();
        SetTool(InkCanvasEditingMode.None);
    }

    private void ResetEditing()
    {
        _changingStrokes = true;
        InkLayer.Strokes.Clear();
        _changingStrokes = false;
        _undo.Clear();
        _lastStrokes = new StrokeCollection();
        _hasUnsavedEdits = false;
    }

    private void StrokesChanged(object? sender, StrokeCollectionChangedEventArgs e)
    {
        if (_changingStrokes) return;
        _undo.Push(_lastStrokes.Clone());
        _lastStrokes = InkLayer.Strokes.Clone();
        _hasUnsavedEdits = true;
        if (_currentIndex >= 0 && _currentIndex < _imagePaths.Count) UpdateMetadata(_imagePaths[_currentIndex]);
    }

    private void SetTool(InkCanvasEditingMode mode)
    {
        InkLayer.EditingMode = mode;
        InkLayer.IsHitTestVisible = mode != InkCanvasEditingMode.None;
        ViewToolButton.BorderBrush = mode == InkCanvasEditingMode.None ? Brushes.Cyan : Brushes.DimGray;
        PenToolButton.BorderBrush = mode == InkCanvasEditingMode.Ink ? Brushes.Cyan : Brushes.DimGray;
        EraserToolButton.BorderBrush = mode == InkCanvasEditingMode.EraseByStroke ? Brushes.Cyan : Brushes.DimGray;
    }

    private void ViewToolButton_Click(object sender, RoutedEventArgs e) => SetTool(InkCanvasEditingMode.None);
    private void PenToolButton_Click(object sender, RoutedEventArgs e) => SetTool(InkCanvasEditingMode.Ink);
    private void EraserToolButton_Click(object sender, RoutedEventArgs e) => SetTool(InkCanvasEditingMode.EraseByStroke);
    private void ColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string value }) _penColor = (Color)ColorConverter.ConvertFromString(value);
        UpdateDrawingAttributes();
        SetTool(InkCanvasEditingMode.Ink);
    }

    private void StrokeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdateDrawingAttributes();
    private void UpdateDrawingAttributes()
    {
        if (InkLayer is null) return;
        var width = StrokeSlider?.Value ?? 4;
        InkLayer.DefaultDrawingAttributes = new DrawingAttributes { Color = _penColor, Width = width, Height = width, FitToCurve = true, IgnorePressure = true };
    }

    private void UndoButton_Click(object sender, RoutedEventArgs e)
    {
        if (_undo.Count == 0) return;
        var previous = _undo.Pop();
        _changingStrokes = true;
        InkLayer.Strokes.Clear();
        InkLayer.Strokes.Add(previous);
        _changingStrokes = false;
        _lastStrokes = InkLayer.Strokes.Clone();
        _hasUnsavedEdits = true;
        UpdateMetadata(_imagePaths[_currentIndex]);
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        if (InkLayer.Strokes.Count > 0) InkLayer.Strokes.Clear();
    }

    private BitmapSource RenderEditedImage()
    {
        if (_currentImage is null) throw new InvalidOperationException("이미지가 없습니다.");
        if (InkLayer.Strokes.Count == 0) return _currentImage;
        var width = _currentImage.PixelWidth;
        var height = _currentImage.PixelHeight;
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawImage(_currentImage, new Rect(0, 0, width, height));
            context.PushTransform(new ScaleTransform(width / _displayWidth, height / _displayHeight));
            InkLayer.Strokes.Draw(context);
            context.Pop();
        }
        var image = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        image.Render(visual);
        image.Freeze();
        return image;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e) => SaveEditedImage();
    private bool SaveEditedImage()
    {
        if (_currentImage is null) return false;
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JPEG 이미지 (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG 이미지 (*.png)|*.png",
            AddExtension = true,
            FileName = "xcapture-" + TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "Korea Standard Time").ToString("yyyyMMdd-HHmmss")
        };
        if (dialog.ShowDialog(this) != true) return false;
        try
        {
            FileService.SaveImage(RenderEditedImage(), dialog.FileName);
            _hasUnsavedEdits = false;
            UpdateMetadata(_imagePaths[_currentIndex]);
            return true;
        }
        catch (Exception ex)
        {
            LogService.Error(ex, "Failed to save edited image");
            MessageBox.Show(this, $"이미지를 저장하지 못했습니다.\n{ex.Message}", "XViewer", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private bool ConfirmDiscardOrSave()
    {
        if (!_hasUnsavedEdits) return true;
        var answer = MessageBox.Show(this, "편집한 내용을 저장하시겠습니까?\n저장하지 않으면 변경 내용이 사라집니다.", "XViewer", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        return answer == MessageBoxResult.No || (answer == MessageBoxResult.Yes && SaveEditedImage());
    }

    private void Window_Closing(object sender, CancelEventArgs e) => e.Cancel = !ConfirmDiscardOrSave();
    private void PrintButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentImage is null) return;
        new PrintPreviewWindow(RenderEditedImage()) { Owner = this }.ShowDialog();
    }
    private void CaptureListButton_Click(object sender, RoutedEventArgs e) => _showMainWindow();
}
