using System.IO;
using System.Windows.Media.Imaging;
using XCapture.Models;

namespace XCapture.Services;

public static class HistoryService
{
    private const int MaxItems = 10;

    private static readonly string HistoryDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "XCapture",
        "History");

    public static void Save(BitmapSource image)
    {
        Directory.CreateDirectory(HistoryDirectory);
        var path = Path.Combine(HistoryDirectory, $"capture-{DateTime.Now:yyyyMMdd-HHmmss-fff}.png");
        FileService.SavePng(image, path);
        Prune();
    }

    public static IReadOnlyList<CaptureHistoryItem> Load()
    {
        Directory.CreateDirectory(HistoryDirectory);
        return Directory
            .GetFiles(HistoryDirectory, "*.png")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.CreationTime)
            .Take(MaxItems)
            .Select(CreateItem)
            .Where(item => item is not null)
            .Cast<CaptureHistoryItem>()
            .ToList();
    }

    public static BitmapImage LoadImage(string path)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(path);
        image.EndInit();
        image.Freeze();
        return image;
    }

    public static void Prune()
    {
        Directory.CreateDirectory(HistoryDirectory);
        foreach (var file in Directory.GetFiles(HistoryDirectory, "*.png")
                     .Select(path => new FileInfo(path))
                     .OrderByDescending(file => file.CreationTime)
                     .Skip(MaxItems))
        {
            try
            {
                file.Delete();
            }
            catch (Exception exc)
            {
                LogService.Error(exc, $"Failed to delete old history file: {file.FullName}");
            }
        }
    }

    private static CaptureHistoryItem? CreateItem(FileInfo file)
    {
        try
        {
            return new CaptureHistoryItem
            {
                FilePath = file.FullName,
                Title = file.Name,
                CreatedText = file.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"),
                Thumbnail = LoadImage(file.FullName)
            };
        }
        catch (Exception exc)
        {
            LogService.Error(exc, $"Failed to load history file: {file.FullName}");
            return null;
        }
    }
}
