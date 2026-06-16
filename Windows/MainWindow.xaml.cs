using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using XCapture.Models;
using XCapture.Services;

namespace XCapture.Windows;

public partial class MainWindow : Window
{
    private readonly Action _regionCapture;
    private readonly Action _fullScreenCapture;
    private readonly Action<BitmapSource, bool, string?> _openEditor;
    private readonly Action _openSettings;

    public ObservableCollection<CaptureHistoryItem> HistoryItems { get; } = new();

    public MainWindow(
        Action regionCapture,
        Action fullScreenCapture,
        Action<BitmapSource, bool, string?> openEditor,
        Action openSettings,
        AppSettings settings)
    {
        InitializeComponent();
        _regionCapture = regionCapture;
        _fullScreenCapture = fullScreenCapture;
        _openEditor = openEditor;
        _openSettings = openSettings;
        DataContext = this;
        UpdateShortcuts(settings);
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

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        await UpdateService.CheckForUpdatesAsync(this, showUpToDateMessage: true);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        _openSettings();
    }

    public void UpdateShortcuts(AppSettings settings)
    {
        ShortcutText.Text =
            $"{settings.RegionCaptureHotKey.DisplayText} 영역 캡쳐, " +
            $"{settings.FullScreenCaptureHotKey.DisplayText} 전체화면캡쳐";
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

    private void DeleteHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not CaptureHistoryItem item)
        {
            return;
        }

        if (System.Windows.MessageBox.Show(
                this,
                "선택한 캡쳐 이미지를 삭제할까요?",
                "XCapture",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            HistoryService.Delete(item.FilePath);
            RefreshHistory();
        }
        catch
        {
            System.Windows.MessageBox.Show(this, "캡쳐 이미지 삭제 중 오류가 발생했습니다.", "XCapture");
        }
    }

    private void OpenSelectedHistory()
    {
        if (HistoryList.SelectedItem is CaptureHistoryItem item)
        {
            OpenHistory(item);
        }
    }

    private void OpenHistory(CaptureHistoryItem item)
    {
        var image = HistoryService.LoadImage(item.FilePath);
        _openEditor(image, false, item.FilePath);
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
