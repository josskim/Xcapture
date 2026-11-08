namespace XCapture.Models;

public sealed class AppSettings
{
    public HotKeyGesture RegionCaptureHotKey { get; set; } = HotKeyGesture.DefaultRegion;
    public HotKeyGesture FullScreenCaptureHotKey { get; set; } = HotKeyGesture.DefaultFullScreen;
    public HotKeyGesture ScrollCaptureHotKey { get; set; } = HotKeyGesture.DefaultScroll;
}
