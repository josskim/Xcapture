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
    private HotKeyGesture _scrollHotKey;

    public AppSettings? Result { get; private set; }

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _regionHotKey = settings.RegionCaptureHotKey;
        _fullScreenHotKey = settings.FullScreenCaptureHotKey;
        _scrollHotKey = settings.ScrollCaptureHotKey;
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
        else if (ReferenceEquals(sender, FullScreenHotKeyBox))
        {
            _fullScreenHotKey = gesture;
        }
        else
        {
            _scrollHotKey = gesture;
        }

        RefreshTexts();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _regionHotKey = HotKeyGesture.DefaultRegion;
        _fullScreenHotKey = HotKeyGesture.DefaultFullScreen;
        _scrollHotKey = HotKeyGesture.DefaultScroll;
        RefreshTexts();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_regionHotKey == _fullScreenHotKey || _regionHotKey == _scrollHotKey || _fullScreenHotKey == _scrollHotKey)
        {
            MessageBox.Show(this, "캡쳐 단축키를 서로 다르게 지정해주세요.", "XCapture");
            return;
        }

        Result = new AppSettings
        {
            RegionCaptureHotKey = _regionHotKey,
            FullScreenCaptureHotKey = _fullScreenHotKey,
            ScrollCaptureHotKey = _scrollHotKey
        };
        DialogResult = true;
    }

    private void RefreshTexts()
    {
        RegionHotKeyBox.Text = _regionHotKey.DisplayText;
        FullScreenHotKeyBox.Text = _fullScreenHotKey.DisplayText;
        ScrollHotKeyBox.Text = _scrollHotKey.DisplayText;
    }
}
