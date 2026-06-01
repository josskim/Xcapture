using System.IO;
using System.Windows.Media.Imaging;

namespace XCapture.Services;

public static class FileService
{
    public static void SavePng(BitmapSource image, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
