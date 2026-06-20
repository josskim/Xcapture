using System.Windows;
using XCapture.Models;
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
    private AppSettings _settings = new();
    private SingleInstanceService? _singleInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _singleInstance = new SingleInstanceService();
        if (!_singleInstance.IsPrimaryInstance)
        {
            Shutdown();
            return;
        }

        _singleInstance.ActivationRequested += ShowMainWindow;
        _singleInstance.StartListening();

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

        _settings = SettingsService.Load();
        _mainWindow = new MainWindow(StartRegionCapture, StartFullScreenCapture, OpenEditor, OpenSettings, _settings);
        MainWindow = _mainWindow;

        _tray = new TrayService(
            ShowMainWindow,
            StartRegionCapture,
            StartFullScreenCapture,
            Shutdown,
            _settings);
        _hotKeys = new HotKeyManager();
        _hotKeys.RegionCapturePressed += StartRegionCapture;
        _hotKeys.FullScreenCapturePressed += StartFullScreenCapture;
        if (!_hotKeys.Register(_settings))
        {
            MessageBox.Show(
                "설정한 단축키를 다른 프로그램에서 사용 중입니다.\n\n설정값은 유지됩니다. 설정에서 다른 단축키를 지정해주세요.",
                "XCapture",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

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

    private void OpenSettings()
    {
        if (_mainWindow is null || _hotKeys is null)
        {
            return;
        }

        var window = new SettingsWindow(_settings)
        {
            Owner = _mainWindow
        };

        if (window.ShowDialog() != true || window.Result is null)
        {
            return;
        }

        if (!_hotKeys.Apply(window.Result))
        {
            MessageBox.Show(
                _mainWindow,
                "다른 프로그램에서 사용 중인 단축키입니다. 다른 키 조합을 선택해주세요.",
                "XCapture",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        _settings = window.Result;
        SettingsService.Save(_settings);
        _mainWindow.UpdateShortcuts(_settings);
        _tray?.UpdateShortcuts(_settings);
    }

    private void OpenEditor(System.Windows.Media.Imaging.BitmapSource bitmap, bool saveToHistory = true, string? historyFilePath = null)
    {
        if (saveToHistory)
        {
            historyFilePath = HistoryService.Save(bitmap);
        }

        ClipboardService.TryCopyImage(bitmap);

        if (_editorWindow is null)
        {
            _editorWindow = new EditorWindow(bitmap, ShowMainWindow, historyFilePath);
            _editorWindow.Closed += (_, _) => _editorWindow = null;
            _editorWindow.Show();
        }
        else
        {
            _editorWindow.ReplaceImage(bitmap, historyFilePath);
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
        _singleInstance?.Dispose();
        _editorWindow = null;
        _mainWindow = null;
        base.OnExit(e);
    }
}
