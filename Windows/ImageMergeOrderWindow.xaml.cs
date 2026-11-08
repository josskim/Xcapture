using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
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
        _items = new ObservableCollection<ImageItem>(paths.Select(path => new ImageItem(path)));
        ImageList.ItemsSource = _items;
        if (_items.Count > 0) ImageList.SelectedIndex = 0;
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = ImageFileService.OpenDialogFilter, Multiselect = true, CheckFileExists = true };
        if (dialog.ShowDialog(this) != true) return;
        foreach (var path in dialog.FileNames.OrderBy(path => Path.GetFileName(path), StringComparer.CurrentCultureIgnoreCase)) _items.Add(new ImageItem(path));
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

    private sealed record ImageItem(string Path)
    {
        public string DisplayName => System.IO.Path.GetFileName(Path) + "  —  " + Path;
    }
}
