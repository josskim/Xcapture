using System.IO;
using System.Windows.Media.Imaging;

namespace XCapture.Services;

public static class FileService
{
    public static void SaveImage(BitmapSource image, string path)
    {
        if (string.Equals(Path.GetExtension(path), ".jpg", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetExtension(path), ".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            SaveJpeg(image, path);
            return;
        }

        SavePng(image, path);
    }

    public static void SavePng(BitmapSource image, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    public static void SaveJpeg(BitmapSource image, string path)
    {
        var encoder = new JpegBitmapEncoder
        {
            QualityLevel = 95
        };
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
