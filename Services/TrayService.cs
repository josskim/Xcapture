using System.Drawing;
using System.IO;
using System.Windows.Forms;
using XCapture.Models;

namespace XCapture.Services;

public sealed class TrayService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _regionCaptureItem;
    private readonly ToolStripMenuItem _fullScreenCaptureItem;
    private AppSettings _settings;

    public TrayService(
        Action showMain,
        Action regionCapture,
        Action fullScreenCapture,
        Action exit,
        AppSettings settings)
    {
        _settings = settings;
        var menu = new ContextMenuStrip();
        menu.Items.Add("XCapture 열기", null, (_, _) => showMain());
        _regionCaptureItem = new ToolStripMenuItem("", null, (_, _) => regionCapture());
        _fullScreenCaptureItem = new ToolStripMenuItem("", null, (_, _) => fullScreenCapture());
        menu.Items.Add(_regionCaptureItem);
        menu.Items.Add(_fullScreenCaptureItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("종료", null, (_, _) => exit());

        _notifyIcon = new NotifyIcon
        {
            Text = "XCapture",
            Icon = LoadTrayIcon(),
            ContextMenuStrip = menu,
            Visible = false
        };

        _notifyIcon.DoubleClick += (_, _) => showMain();
        UpdateShortcuts(settings);
    }

    public void Show()
    {
        _notifyIcon.Visible = true;
        _notifyIcon.ShowBalloonTip(
            1500,
            "XCapture",
            $"메인 창에서 캡쳐하거나 {_settings.RegionCaptureHotKey.DisplayText}를 누르세요.",
            ToolTipIcon.Info);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }

    public void UpdateShortcuts(AppSettings settings)
    {
        _settings = settings;
        _regionCaptureItem.Text = $"영역 캡쳐 ({settings.RegionCaptureHotKey.DisplayText})";
        _fullScreenCaptureItem.Text = $"전체 화면 캡쳐 ({settings.FullScreenCaptureHotKey.DisplayText})";
    }

    private static Icon LoadTrayIcon()
    {
        try
        {
            var appDirectory = AppContext.BaseDirectory;
            var iconPath = Path.Combine(appDirectory, "Assets", "app.ico");
            if (File.Exists(iconPath))
            {
                return new Icon(iconPath);
            }

            var executablePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return SystemIcons.Application;
            }

            return Icon.ExtractAssociatedIcon(executablePath) ?? SystemIcons.Application;
        }
        catch
        {
            return SystemIcons.Application;
        }
    }
}
