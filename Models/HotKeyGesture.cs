using System.Windows.Input;

namespace XCapture.Models;

public sealed record HotKeyGesture(bool Control, bool Shift, bool Alt, bool Windows, Key Key)
{
    public static HotKeyGesture DefaultRegion { get; } = new(true, true, false, false, Key.S);
    public static HotKeyGesture DefaultFullScreen { get; } = new(true, true, false, false, Key.A);

    public bool HasModifier => Control || Shift || Alt || Windows;

    public string DisplayText
    {
        get
        {
            var parts = new List<string>();
            if (Control) parts.Add("Ctrl");
            if (Shift) parts.Add("Shift");
            if (Alt) parts.Add("Alt");
            if (Windows) parts.Add("Win");
            parts.Add(Key.ToString());
            return string.Join("+", parts);
        }
    }
}
