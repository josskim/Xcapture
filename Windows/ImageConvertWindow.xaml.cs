using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using XCapture.Services;
using MessageBox = System.Windows.MessageBox;

namespace XCapture.Windows;

public partial class ImageConvertWindow : Window
{
    private readonly BitmapSource _image;
    private readonly string _sourcePath;
    private bool _updating;

    public ImageConvertWindow(BitmapSource image, string sourcePath)
    {
        InitializeComponent();
        _image = image;
        _sourcePath = sourcePath;
        WidthBox.Text = image.PixelWidth.ToString();
        HeightBox.Text = image.PixelHeight.ToString();
        WidthBox.TextChanged += WidthChanged;
        HeightBox.TextChanged += HeightChanged;
    }

    private void WidthChanged(object sender, TextChangedEventArgs e)
    {
        if (!_updating && KeepRatio.IsChecked == true && int.TryParse(WidthBox.Text, out var width) && width > 0 && width <= 100000)
        {
            _updating = true;
            HeightBox.Text = Math.Max(1, (int)Math.Round(width * (double)_image.PixelHeight / _image.PixelWidth)).ToString();
            _updating = false;
        }
    }

    private void HeightChanged(object sender, TextChangedEventArgs e)
    {
        if (!_updating && KeepRatio.IsChecked == true && int.TryParse(HeightBox.Text, out var height) && height > 0 && height <= 100000)
        {
            _updating = true;
            WidthBox.Text = Math.Max(1, (int)Math.Round(height * (double)_image.PixelWidth / _image.PixelHeight)).ToString();
            _updating = false;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(WidthBox.Text, out var width) || !int.TryParse(HeightBox.Text, out var height) || !int.TryParse(QualityBox.Text, out var quality) || width < 1 || height < 1 || quality is < 1 or > 100 || (long)width * height > 100_000_000)
        {
            MessageBox.Show(this, "크기와 JPEG 품질을 확인해주세요.", "XViewer");
            return;
        }
        var save = new Microsoft.Win32.SaveFileDialog { Filter = "JPEG 이미지 (*.jpg)|*.jpg|PNG 이미지 (*.png)|*.png|BMP 이미지 (*.bmp)|*.bmp", AddExtension = true, FileName = Path.GetFileNameWithoutExtension(_sourcePath) + "-converted" };
        if (save.ShowDialog(this) != true) return;
        if (string.Equals(Path.GetFullPath(save.FileName), Path.GetFullPath(_sourcePath), StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(this, "원본과 다른 이름으로 저장해주세요.", "XViewer");
            return;
        }
        try
        {
            ImageOperations.Save(ImageOperations.Resize(_image, width, height), save.FileName, quality);
            var kb = new FileInfo(save.FileName).Length / 1024d;
            MessageBox.Show(this, $"저장했습니다. ({kb:N1} KB)\n{save.FileName}", "XViewer");
            DialogResult = true;
        }
        catch (Exception ex) { LogService.Error(ex, "Image conversion failed"); MessageBox.Show(this, $"변환할 수 없습니다.\n{ex.Message}", "XViewer"); }
    }
}
