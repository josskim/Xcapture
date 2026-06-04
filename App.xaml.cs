using System.Windows;
using XCapture.Services;
using XCapture.Windows;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace XCapture;

public partial class App : Application
{
    private HotKeyManager? _hotKeys;
    private TrayService? _tray;
    private MainWindow? _mainWindow;
    private EditorWindow? _editorWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            LogService.Error(args.Exception, "Dispatcher unhandled exception");
            MessageBox.Show(
                $"오류가 발생했습니다.\n\n로그: {LogService.LogPath}",
                "XCapture",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        _mainWindow = new MainWindow(StartRegionCapture, StartFullScreenCapture, OpenEditor);
        MainWindow = _mainWindow;

        _tray = new TrayService(ShowMainWindow, StartRegionCapture, StartFullScreenCapture, Shutdown);
        _hotKeys = new HotKeyManager();
        _hotKeys.RegionCapturePressed += StartRegionCapture;
        _hotKeys.FullScreenCapturePressed += StartFullScreenCapture;
        _hotKeys.Register();

        _tray.Show();
        _mainWindow.ShowAndActivate();
        _ = UpdateService.CheckForUpdatesAsync(_mainWindow);
    }

    private void StartRegionCapture()
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                var overlay = new CaptureOverlayWindow();
                overlay.ShowDialog();
                if (overlay.IsCaptureAccepted && overlay.CapturedBitmap is not null)
                {
                    CaptureSoundService.PlayShutter();
                    OpenEditor(overlay.CapturedBitmap);
                    _mainWindow?.RefreshHistory();
                }
            }
            catch (Exception exc)
            {
                LogService.Error(exc, "Region capture failed");
                MessageBox.Show($"캡쳐 중 오류가 발생했습니다.\n\n로그: {LogService.LogPath}", "XCapture");
            }
        });
    }

    private void StartFullScreenCapture()
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                var bitmap = ScreenCaptureService.CaptureAllScreens();
                CaptureSoundService.PlayShutter();
                OpenEditor(bitmap);
                _mainWindow?.RefreshHistory();
            }
            catch (Exception exc)
            {
                LogService.Error(exc, "Full screen capture failed");
                MessageBox.Show($"캡쳐 중 오류가 발생했습니다.\n\n로그: {LogService.LogPath}", "XCapture");
            }
        });
    }

    private void ShowMainWindow()
    {
        Dispatcher.Invoke(() => _mainWindow?.ShowAndActivate());
    }

    private void OpenEditor(System.Windows.Media.Imaging.BitmapSource bitmap, bool saveToHistory = true)
    {
        if (saveToHistory)
        {
            HistoryService.Save(bitmap);
        }

        ClipboardService.TryCopyImage(bitmap);

        if (_editorWindow is null)
        {
            _editorWindow = new EditorWindow(bitmap, ShowMainWindow);
            _editorWindow.Closed += (_, _) => _editorWindow = null;
            _editorWindow.Show();
        }
        else
        {
            _editorWindow.ReplaceImage(bitmap);
            if (!_editorWindow.IsVisible)
            {
                _editorWindow.Show();
            }
        }

        _editorWindow.WindowState = WindowState.Normal;
        _editorWindow.Topmost = true;
        _editorWindow.Activate();
        _editorWindow.Topmost = false;
        _editorWindow.Focus();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotKeys?.Dispose();
        _tray?.Dispose();
        _editorWindow = null;
        _mainWindow = null;
        base.OnExit(e);
    }
}
