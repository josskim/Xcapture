using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using XCapture.Services;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;
using DragEventArgs = System.Windows.DragEventArgs;
using DragDropEffects = System.Windows.DragDropEffects;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace XCapture.Windows;

public partial class ImageViewerWindow : Window
{
    private const double MinZoom = 0.05;
    private const double MaxZoom = 8.0;
    private const double ZoomStep = 1.2;

    private readonly Action _showMainWindow;
    private bool _historyMode;
    private IReadOnlyList<string> _imagePaths = [];
    private int _currentIndex = -1;
    private BitmapSource? _currentImage;
    private bool _fitToWindow = true;
    private double _zoom = 1.0;
    private double _displayWidth = 1.0;
    private double _displayHeight = 1.0;

    public ImageViewerWindow(Action showMainWindow)
    {
        InitializeComponent();
        _showMainWindow = showMainWindow;
        InitializeEditing();
    }

    public bool OpenImage(string path, IReadOnlyList<string>? requestedImages = null, bool historyMode = false)
    {
        if (!ConfirmDiscardOrSave()) return false;
        var normalized = ImageFileService.NormalizePath(path);
        var candidates = requestedImages is { Count: > 0 }
            ? ImageFileService.NormalizeExistingImages(requestedImages)
            : ImageFileService.LoadDirectoryImages(normalized);

        var image = ImageFileService.LoadImage(normalized);
        _imagePaths = candidates.Contains(normalized, StringComparer.OrdinalIgnoreCase)
            ? candidates
            : [normalized];
        _currentIndex = FindIndex(normalized);
        if (_currentIndex < 0)
        {
            _currentIndex = 0;
        }

        _historyMode = historyMode;
        SetCurrentImage(image, normalized);
        ShowAndActivate();
        return true;
    }

    public void ShowAndActivate()
    {
        if (!IsVisible)
        {
            Show();
        }

        WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    private void SetCurrentImage(BitmapSource image, string path)
    {
        ResetEditing();
        _currentImage = image;
        ViewerImage.Source = image;
        UpdateDisplaySize();
        _fitToWindow = true;
        FitImageToWindow();
        UpdateMetadata(path);
    }

    private void UpdateMetadata(string path)
    {
        var file = new FileInfo(path);
        FileNameText.Text = file.Name;
        Title = $"{file.Name}{(_hasUnsavedEdits ? " *" : "")} - XViewer";
        ImageInfoText.Text = _currentImage is null
            ? file.FullName
            : $"{_currentImage.PixelWidth:N0} × {_currentImage.PixelHeight:N0} · {(file.Exists ? FormatFileSize(file.Length) : "파일 없음")} · {file.FullName}";
        PositionText.Text = _imagePaths.Count > 0 ? $"{_currentIndex + 1} / {_imagePaths.Count}" : "";
        PreviousButton.IsEnabled = _currentIndex > 0;
        NextButton.IsEnabled = _currentIndex >= 0 && _currentIndex < _imagePaths.Count - 1;
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes >= 1024 * 1024)
        {
            return $"{bytes / 1024d / 1024d:0.##} MB";
        }

        return $"{Math.Max(1, bytes / 1024d):0.#} KB";
    }

    private int FindIndex(string path)
    {
        for (var index = 0; index < _imagePaths.Count; index++)
        {
            if (string.Equals(_imagePaths[index], path, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private void Navigate(int offset)
    {
        if (_historyMode)
        {
            var current = _currentIndex >= 0 && _currentIndex < _imagePaths.Count ? _imagePaths[_currentIndex] : null;
            var fresh = HistoryService.Load().Select(item => item.FilePath).ToList();
            var currentIndex = current is null ? -1 : fresh.FindIndex(p => string.Equals(p, current, StringComparison.OrdinalIgnoreCase));
            if (currentIndex >= 0) { _imagePaths = fresh; _currentIndex = currentIndex; }
        }
        var target = _currentIndex + offset;
        if (target < 0 || target >= _imagePaths.Count)
        {
            return;
        }

        if (!ConfirmDiscardOrSave()) return;
        var previousIndex = _currentIndex;
        _currentIndex = target;
        try { SetCurrentImage(ImageFileService.LoadImage(_imagePaths[target]), _imagePaths[target]); }
        catch (Exception exc)
        {
            _currentIndex = previousIndex;
            LogService.Error(exc, "Failed to navigate image");
            MessageBox.Show(this, "이미지를 열 수 없습니다.", "XViewer", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = ImageFileService.OpenDialogFilter,
            Multiselect = true,
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) == true && dialog.FileNames.Length > 0)
        {
            OpenImage(dialog.FileNames[0], dialog.FileNames);
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentImage is not null)
        {
            ClipboardService.TryCopyImage(RenderEditedImage());
        }
    }

    private void CopyContextMenuItem_Click(object sender, RoutedEventArgs e)
    {
        CopyButton_Click(sender, e);
    }

    private void OpenLocationButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentIndex < 0)
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{_imagePaths[_currentIndex]}\"",
            UseShellExecute = true
        });
    }

    private void MergeButton_Click(object sender, RoutedEventArgs e)
    {
        var open = new OpenFileDialog { Filter = ImageFileService.OpenDialogFilter, Multiselect = true, CheckFileExists = true, Title = "합칠 이미지 선택 (파일 이름순)" };
        if (open.ShowDialog(this) != true || open.FileNames.Length == 0) return;
        var order = new ImageMergeOrderWindow(open.FileNames.OrderBy(x => Path.GetFileName(x), StringComparer.CurrentCultureIgnoreCase)) { Owner = this };
        if (order.ShowDialog() != true) return;
        var paths = order.Paths;
        var save = new Microsoft.Win32.SaveFileDialog { Filter = "JPEG 이미지 (*.jpg)|*.jpg|PNG 이미지 (*.png)|*.png|PDF 문서 (*.pdf)|*.pdf", AddExtension = true, FileName = "merged" };
        if (save.ShowDialog(this) != true) return;
        if (paths.Any(path => string.Equals(Path.GetFullPath(path), Path.GetFullPath(save.FileName), StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(this, "원본 이미지와 다른 이름으로 저장해주세요.", "XViewer");
            return;
        }
        try
        {
            var images = paths.Select(ImageFileService.LoadImage).ToArray();
            if (Path.GetExtension(save.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase)) ImageOperations.SavePdf(images, save.FileName);
            else ImageOperations.Save(ImageOperations.MergeVertical(images), save.FileName, 95);
            MessageBox.Show(this, $"{images.Length}개 이미지를 저장했습니다.\n{save.FileName}", "XViewer");
        }
        catch (Exception ex) { LogService.Error(ex, "Image merge failed"); MessageBox.Show(this, $"이미지를 합칠 수 없습니다.\n{ex.Message}", "XViewer"); }
    }

    private void ConvertButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentImage is null) return;
        var dialog = new ImageConvertWindow(RenderEditedImage(), _imagePaths[_currentIndex]) { Owner = this };
        dialog.ShowDialog();
    }

    private void PreviousButton_Click(object sender, RoutedEventArgs e) => Navigate(-1);

    private void NextButton_Click(object sender, RoutedEventArgs e) => Navigate(1);

    private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
    {
        _fitToWindow = false;
        SetZoom(_zoom / ZoomStep);
    }

    private void ZoomInButton_Click(object sender, RoutedEventArgs e)
    {
        _fitToWindow = false;
        SetZoom(_zoom * ZoomStep);
    }

    private void FitButton_Click(object sender, RoutedEventArgs e)
    {
        _fitToWindow = true;
        FitImageToWindow();
    }

    private void ActualSizeButton_Click(object sender, RoutedEventArgs e)
    {
        _fitToWindow = false;
        SetZoom(1.0);
    }

    private void ImageScrollViewer_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateDisplaySize();
        FitImageToWindow();
    }

    private void ImageScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_fitToWindow)
        {
            FitImageToWindow();
        }
    }

    private void ImageScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control)
        {
            return;
        }

        _fitToWindow = false;
        SetZoom(e.Delta > 0 ? _zoom * ZoomStep : _zoom / ZoomStep);
        e.Handled = true;
    }

    private void UpdateDisplaySize()
    {
        if (_currentImage is null)
        {
            return;
        }

        var dpi = PresentationSource.FromVisual(this) is null
            ? new DpiScale(1.0, 1.0)
            : VisualTreeHelper.GetDpi(this);
        _displayWidth = Math.Max(1, _currentImage.PixelWidth / dpi.DpiScaleX);
        _displayHeight = Math.Max(1, _currentImage.PixelHeight / dpi.DpiScaleY);
        ImageSurface.Width = _displayWidth;
        ImageSurface.Height = _displayHeight;
        ViewerImage.Width = _displayWidth;
        ViewerImage.Height = _displayHeight;
        InkLayer.Width = _displayWidth;
        InkLayer.Height = _displayHeight;
    }

    private void FitImageToWindow()
    {
        if (_currentImage is null)
        {
            return;
        }

        var width = Math.Max(1, ImageScrollViewer.ActualWidth - 64);
        var height = Math.Max(1, ImageScrollViewer.ActualHeight - 64);
        SetZoom(Math.Min(1.0, Math.Min(width / _displayWidth, height / _displayHeight)));
    }

    private void SetZoom(double value)
    {
        _zoom = Math.Clamp(value, MinZoom, MaxZoom);
        ImageScale.ScaleX = _zoom;
        ImageScale.ScaleY = _zoom;
        ZoomText.Text = $"{Math.Round(_zoom * 100)}%";
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Left && Keyboard.Modifiers == ModifierKeys.None && Keyboard.FocusedElement is not System.Windows.Controls.Slider)
        {
            Navigate(-1);
            e.Handled = true;
        }
        else if (e.Key == Key.Right && Keyboard.Modifiers == ModifierKeys.None && Keyboard.FocusedElement is not System.Windows.Controls.Slider)
        {
            Navigate(1);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.O)
        {
            OpenButton_Click(sender, e);
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
        {
            CopyButton_Click(sender, e);
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
        {
            SaveEditedImage();
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
        else if (e.Key == Key.Add || e.Key == Key.OemPlus)
        {
            ZoomInButton_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.Subtract || e.Key == Key.OemMinus)
        {
            ZoomOutButton_Click(sender, e);
            e.Handled = true;
        }
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files)
        {
            return;
        }

        var images = ImageFileService.NormalizeExistingImages(files);
        if (images.Count > 0)
        {
            OpenImage(images[0], images);
        }
    }
}
