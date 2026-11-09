using Microsoft.Win32;
using System.IO;
using System.Windows;
using XCapture.Models;
using XCapture.Services;
using XCapture.Windows;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace XCapture;

public partial class App : Application
{
    private HotKeyManager? _hotKeys;
    private TrayService? _tray;
    private MainWindow? _mainWindow;
    private ImageViewerWindow? _imageViewerWindow;
    private AppSettings _settings = new();
    private SingleInstanceService? _singleInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _singleInstance = new SingleInstanceService();
        if (!_singleInstance.IsPrimaryInstance)
        {
            _singleInstance.RequestActivation(e.Args);
            Shutdown();
            return;
        }

        _singleInstance.ActivationRequested += HandleActivationRequest;
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
        _mainWindow = new MainWindow(StartRegionCapture, StartFullScreenCapture, StartScrollCapture, OpenCaptureImage, OpenImageDialog, OpenSettings, _settings);
        MainWindow = _mainWindow;

        _tray = new TrayService(
            ShowMainWindow,
            StartRegionCapture,
            StartFullScreenCapture,
            StartScrollCapture,
            ExitApplication,
            _settings);
        _hotKeys = new HotKeyManager();
        _hotKeys.RegionCapturePressed += StartRegionCapture;
        _hotKeys.FullScreenCapturePressed += StartFullScreenCapture;
        _hotKeys.ScrollCapturePressed += StartScrollCapture;
        if (!_hotKeys.Register(_settings))
        {
            MessageBox.Show(
                "설정한 단축키를 다른 프로그램에서 사용 중입니다.\n\n설정값은 유지됩니다. 설정에서 다른 단축키를 지정해주세요.",
                "XCapture",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        _tray.Show();
        var startupImages = ImageFileService.NormalizeExistingImages(e.Args);
        if (startupImages.Count > 0)
        {
            OpenImageViewer(startupImages[0], startupImages.Count > 1 ? startupImages : null);
        }
        else
        {
            _mainWindow.ShowAndActivate();
        }

        Window updateOwner = _imageViewerWindow is not null ? _imageViewerWindow : _mainWindow;
        _ = UpdateService.CheckForUpdatesAsync(updateOwner);
    }

    private void StartRegionCapture()
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                // Freeze the screen before the overlay takes focus so transient
                // browser menus and popups remain visible in the capture.
                var snapshot = ScreenCaptureService.CaptureAllScreens();
                var overlay = new CaptureOverlayWindow(snapshot);
                overlay.ShowDialog();
                if (overlay.IsCaptureAccepted && overlay.CapturedBitmap is not null)
                {
                    CaptureSoundService.PlayShutter();
                    OpenCaptureImage(overlay.CapturedBitmap);
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
                OpenCaptureImage(bitmap);
                _mainWindow?.RefreshHistory();
            }
            catch (Exception exc)
            {
                LogService.Error(exc, "Full screen capture failed");
                MessageBox.Show($"캡쳐 중 오류가 발생했습니다.\n\n로그: {LogService.LogPath}", "XCapture");
            }
        });
    }

    private async void StartScrollCapture()
    {
        try
        {
            if (_mainWindow?.IsActive == true) _mainWindow.WindowState = WindowState.Minimized;
            await Task.Delay(300);
            var overlay = new CaptureOverlayWindow(ScreenCaptureService.CaptureAllScreens(), scrollCapture: true);
            overlay.ShowDialog();
            if (!overlay.IsCaptureAccepted) return;
            await Task.Delay(350);
            var bounds = ScrollCaptureTarget.Resolve(overlay.SelectedScreenBounds);
            var (image, warning) = await ScrollCaptureService.CaptureAsync(bounds, overlay.SelectedScreenBounds);
            CaptureSoundService.PlayShutter();
            OpenCaptureImage(image);
            _mainWindow?.RefreshHistory();
            if (warning is not null) MessageBox.Show(warning, "스크롤 캡처", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception exc)
        {
            LogService.Error(exc, "Scroll capture failed");
            MessageBox.Show($"스크롤 캡처 중 오류가 발생했습니다.\n\n{exc.Message}", "XCapture");
        }
    }

    private void ShowMainWindow()
    {
        Dispatcher.Invoke(() => _mainWindow?.ShowAndActivate());
    }

    private void ExitApplication()
    {
        if (_imageViewerWindow is not null)
        {
            _imageViewerWindow.Close();
            if (_imageViewerWindow is not null) return;
        }
        Shutdown();
    }

    private void HandleActivationRequest(IReadOnlyList<string> arguments)
    {
        Dispatcher.Invoke(() =>
        {
            var images = ImageFileService.NormalizeExistingImages(arguments);
            if (images.Count > 0)
            {
                OpenImageViewer(images[0], images.Count > 1 ? images : null);
                return;
            }

            ShowMainWindow();
        });
    }

    private void OpenImageDialog()
    {
        var dialog = new OpenFileDialog
        {
            Filter = ImageFileService.OpenDialogFilter,
            Multiselect = true,
            CheckFileExists = true
        };
        if (dialog.ShowDialog(_mainWindow) == true && dialog.FileNames.Length > 0)
        {
            OpenImageViewer(dialog.FileNames[0], dialog.FileNames);
        }
    }

    private void OpenImageViewer(string path, IReadOnlyList<string>? imagePaths = null)
    {
        try
        {
            EnsureImageViewer().OpenImage(path, imagePaths);
        }
        catch (Exception exc)
        {
            LogService.Error(exc, $"Failed to open image viewer: {path}");
            MessageBox.Show(
                $"이미지를 열 수 없습니다.\n\n{Path.GetFileName(path)}\n\n로그: {LogService.LogPath}",
                "XCapture 이미지 뷰어",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private ImageViewerWindow EnsureImageViewer()
    {
        if (_imageViewerWindow is null)
        {
            _imageViewerWindow = new ImageViewerWindow(ShowMainWindow);
            _imageViewerWindow.Closed += (_, _) => _imageViewerWindow = null;
        }
        return _imageViewerWindow;
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

    private void OpenCaptureImage(System.Windows.Media.Imaging.BitmapSource bitmap, bool saveToHistory = true, string? historyFilePath = null)
    {
        if (saveToHistory)
        {
            historyFilePath = HistoryService.Save(bitmap);
        }

        ClipboardService.TryCopyImage(bitmap);

        if (historyFilePath is not null)
        {
            var paths = HistoryService.Load().Select(item => item.FilePath).ToList();
            EnsureImageViewer().OpenImage(historyFilePath, paths, historyMode: true);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotKeys?.Dispose();
        _tray?.Dispose();
        _singleInstance?.Dispose();
        _imageViewerWindow = null;
        _mainWindow = null;
        base.OnExit(e);
    }
}
