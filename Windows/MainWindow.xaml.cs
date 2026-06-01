using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using XCapture.Models;
using XCapture.Services;

namespace XCapture.Windows;

public partial class MainWindow : Window
{
    private readonly Action _regionCapture;
    private readonly Action _fullScreenCapture;

    public ObservableCollection<CaptureHistoryItem> HistoryItems { get; } = new();

    public MainWindow(Action regionCapture, Action fullScreenCapture)
    {
        InitializeComponent();
        _regionCapture = regionCapture;
        _fullScreenCapture = fullScreenCapture;
        DataContext = this;
        RefreshHistory();
    }

    public void RefreshHistory()
    {
        HistoryItems.Clear();
        foreach (var item in HistoryService.Load())
        {
            HistoryItems.Add(item);
        }
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
        RefreshHistory();
    }

    private void RegionCaptureButton_Click(object sender, RoutedEventArgs e)
    {
        _regionCapture();
    }

    private void FullScreenCaptureButton_Click(object sender, RoutedEventArgs e)
    {
        _fullScreenCapture();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshHistory();
    }

    private void HistoryList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OpenSelectedHistory();
    }

    private void OpenHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is CaptureHistoryItem item)
        {
            OpenHistory(item);
        }
    }

    private void OpenSelectedHistory()
    {
        if (HistoryList.SelectedItem is CaptureHistoryItem item)
        {
            OpenHistory(item);
        }
    }

    private static void OpenHistory(CaptureHistoryItem item)
    {
        var image = HistoryService.LoadImage(item.FilePath);
        var editor = new EditorWindow(image);
        editor.Show();
        editor.Activate();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
