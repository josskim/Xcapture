using System.Windows.Media.Imaging;

namespace XCapture.Models;

public sealed class CaptureHistoryItem
{
    public required string FilePath { get; init; }
    public required string Title { get; init; }
    public required string CreatedText { get; init; }
    public required BitmapImage Thumbnail { get; init; }
}
