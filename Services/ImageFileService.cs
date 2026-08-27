using System.IO;
using System.Windows.Media.Imaging;

namespace XCapture.Services;

public static class ImageFileService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".bmp",
        ".gif",
        ".tif",
        ".tiff"
    };

    public const string OpenDialogFilter =
        "이미지 파일 (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|모든 파일 (*.*)|*.*";

    public static bool IsSupportedImage(string? path)
    {
        return !string.IsNullOrWhiteSpace(path) &&
               SupportedExtensions.Contains(Path.GetExtension(path));
    }

    public static IReadOnlyList<string> NormalizeExistingImages(IEnumerable<string> paths)
    {
        return paths
            .Where(IsSupportedImage)
            .Select(NormalizePath)
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<string> LoadDirectoryImages(string path)
    {
        var normalized = NormalizePath(path);
        var directory = Path.GetDirectoryName(normalized);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return File.Exists(normalized) ? [normalized] : [];
        }

        return Directory
            .EnumerateFiles(directory)
            .Where(IsSupportedImage)
            .OrderBy(file => Path.GetFileName(file), StringComparer.CurrentCultureIgnoreCase)
            .Select(NormalizePath)
            .ToList();
    }

    public static BitmapSource LoadImage(string path)
    {
        var normalized = NormalizePath(path);
        using var stream = new FileStream(normalized, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        if (decoder.Frames.Count == 0)
        {
            throw new InvalidDataException("이미지 프레임을 읽을 수 없습니다.");
        }

        var frame = decoder.Frames[0];
        frame.Freeze();
        return frame;
    }

    public static string NormalizePath(string path)
    {
        return Path.GetFullPath(path.Trim().Trim('"'));
    }
}
