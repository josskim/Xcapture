using System.Windows;
using System.Windows.Media.Imaging;

namespace XCapture.Services;

public static class ClipboardService
{
    public static bool TryCopyImage(BitmapSource image)
    {
        try
        {
            System.Windows.Clipboard.SetImage(image);
            return true;
        }
        catch (Exception exc)
        {
            LogService.Error(exc, "Clipboard.SetImage failed");
            return false;
        }
    }
}
