using System.Windows;
using System.Windows.Input;
using XCapture.Models;
using XCapture.Services;
using MessageBox = System.Windows.MessageBox;

namespace XCapture.Windows;

public partial class SettingsWindow : Window
{
    private HotKeyGesture _regionHotKey;
    private HotKeyGesture _fullScreenHotKey;

    public AppSettings? Result { get; private set; }

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _regionHotKey = settings.RegionCaptureHotKey;
        _fullScreenHotKey = settings.FullScreenCaptureHotKey;
        RefreshTexts();
    }

    private void HotKeyBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or
            Key.LeftShift or Key.RightShift or
            Key.LeftAlt or Key.RightAlt or
            Key.LWin or Key.RWin)
        {
            return;
        }

        var modifiers = Keyboard.Modifiers;
        var gesture = new HotKeyGesture(
            modifiers.HasFlag(ModifierKeys.Control),
            modifiers.HasFlag(ModifierKeys.Shift),
            modifiers.HasFlag(ModifierKeys.Alt),
            modifiers.HasFlag(ModifierKeys.Windows),
            key);

        if (!SettingsService.IsValid(gesture))
        {
            MessageBox.Show(this, "Ctrl, Shift, Alt, Win 중 하나 이상을 함께 눌러주세요.", "XCapture");
            return;
        }

        if (ReferenceEquals(sender, RegionHotKeyBox))
        {
            _regionHotKey = gesture;
        }
        else
        {
            _fullScreenHotKey = gesture;
        }

        RefreshTexts();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _regionHotKey = HotKeyGesture.DefaultRegion;
        _fullScreenHotKey = HotKeyGesture.DefaultFullScreen;
        RefreshTexts();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_regionHotKey == _fullScreenHotKey)
        {
            MessageBox.Show(this, "영역 캡쳐와 전체화면 캡쳐는 서로 다른 단축키를 지정해주세요.", "XCapture");
            return;
        }

        Result = new AppSettings
        {
            RegionCaptureHotKey = _regionHotKey,
            FullScreenCaptureHotKey = _fullScreenHotKey
        };
        DialogResult = true;
    }

    private void RefreshTexts()
    {
        RegionHotKeyBox.Text = _regionHotKey.DisplayText;
        FullScreenHotKeyBox.Text = _fullScreenHotKey.DisplayText;
    }
}
