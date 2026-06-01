using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace XCapture.Services;

public sealed class TrayService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;

    public TrayService(Action showMain, Action regionCapture, Action fullScreenCapture, Action exit)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("XCapture 열기", null, (_, _) => showMain());
        menu.Items.Add("영역 캡쳐 (Ctrl+Shift+S)", null, (_, _) => regionCapture());
        menu.Items.Add("전체 화면 캡쳐 (Ctrl+Shift+A)", null, (_, _) => fullScreenCapture());
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
    }

    public void Show()
    {
        _notifyIcon.Visible = true;
        _notifyIcon.ShowBalloonTip(1500, "XCapture", "메인 창에서 캡쳐하거나 Ctrl+Shift+S를 누르세요.", ToolTipIcon.Info);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
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
