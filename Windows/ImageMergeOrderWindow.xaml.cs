using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using XCapture.Services;
using MessageBox = System.Windows.MessageBox;

namespace XCapture.Windows;

public partial class ImageMergeOrderWindow : Window
{
    private readonly ObservableCollection<ImageItem> _items;
    public string[] Paths => _items.Select(item => item.Path).ToArray();

    public ImageMergeOrderWindow(IEnumerable<string> paths)
    {
        InitializeComponent();
        _items = new ObservableCollection<ImageItem>(paths.Select(CreateItem));
        ImageList.ItemsSource = _items;
        if (_items.Count > 0) ImageList.SelectedIndex = 0;
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = ImageFileService.OpenDialogFilter, Multiselect = true, CheckFileExists = true };
        if (dialog.ShowDialog(this) != true) return;
        foreach (var path in dialog.FileNames.OrderBy(path => Path.GetFileName(path), StringComparer.CurrentCultureIgnoreCase)) _items.Add(CreateItem(path));
    }

    private void Up_Click(object sender, RoutedEventArgs e) => Move(-1);
    private void Down_Click(object sender, RoutedEventArgs e) => Move(1);
    private void Move(int direction)
    {
        var index = ImageList.SelectedIndex;
        var target = index + direction;
        if (index < 0 || target < 0 || target >= _items.Count) return;
        _items.Move(index, target);
        ImageList.SelectedIndex = target;
        ImageList.ScrollIntoView(_items[target]);
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        var index = ImageList.SelectedIndex;
        if (index < 0) return;
        _items.RemoveAt(index);
        if (_items.Count > 0) ImageList.SelectedIndex = Math.Min(index, _items.Count - 1);
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        if (_items.Count == 0) { MessageBox.Show(this, "이미지를 한 장 이상 선택해주세요.", "XViewer"); return; }
        DialogResult = true;
    }

    private void ImageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ImageList.SelectedItem is ImageItem item)
        {
            try { PreviewImage.Source = ImageFileService.LoadImage(item.Path); PreviewText.Text = item.Name + "   " + item.SizeText; }
            catch (Exception ex) { PreviewImage.Source = null; PreviewText.Text = "미리보기를 불러올 수 없습니다: " + ex.Message; }
        }
        else { PreviewImage.Source = null; PreviewText.Text = "이미지를 선택해주세요."; }
    }

    private static ImageItem CreateItem(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var bitmap = new BitmapImage();
            bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.DecodePixelWidth = 150; bitmap.StreamSource = stream; bitmap.EndInit(); bitmap.Freeze();
            var original = ImageFileService.LoadImage(path);
            return new ImageItem(path, bitmap, $"{original.PixelWidth:N0} × {original.PixelHeight:N0}");
        }
        catch (Exception) { return new ImageItem(path, null, "미리보기 불가"); }
    }

    private sealed record ImageItem(string Path, BitmapSource? Thumbnail, string SizeText)
    {
        public string Name => System.IO.Path.GetFileName(Path);
    }
}
