using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace XCapture.Services;

public static class ScrollCaptureService
{
    public static async Task<(BitmapSource Image, string? Warning)> CaptureAsync(Rectangle bounds, System.Drawing.Point scrollPoint)
    {
        SetCursorPos(scrollPoint.X, scrollPoint.Y);
        var before = ScreenCaptureService.CaptureRectangle(bounds);
        var reachedTop = false;
        for (var attempt = 0; attempt < 100; attempt++)
        {
            ScrollWheel(720);
            await Task.Delay(250);
            var after = ScreenCaptureService.CaptureRectangle(bounds);
            if (FramesMatch(before, after)) { reachedTop = true; break; }
            before = after;
        }
        if (!reachedTop) throw new InvalidOperationException("스크롤 화면의 시작점을 찾지 못했습니다. 페이지를 맨 위로 이동한 뒤 다시 시도해주세요.");
        var frames = new List<(BitmapSource Image, int Skip)> { (before, 0) };
        var totalHeight = before.PixelHeight;
        string? warning = null;
        for (var page = 0; page < 500; page++)
        {
            ScrollWheel(unchecked((uint)-120));
            await Task.Delay(650);
            var next = ScreenCaptureService.CaptureRectangle(bounds);
            if (FramesMatch(frames[^1].Image, next))
            {
                if (frames.Count == 1) warning = "스크롤로 화면이 이동하지 않았습니다. 스크롤 가능한 내용 영역을 선택했는지 확인해주세요.";
                break;
            }

            var overlap = FindOverlap(frames[^1].Image, next);
            if (overlap < 0)
            {
                warning = "스크롤 화면의 겹치는 부분을 확인할 수 없어 여기까지 캡처했습니다. 고정 메뉴나 스크롤바를 제외하고 다시 선택해 보세요.";
                break;
            }

            var addedHeight = next.PixelHeight - overlap;
            var nextHeight = totalHeight + addedHeight;
            if (nextHeight > 60000 || (long)bounds.Width * nextHeight > 120_000_000)
            {
                warning = "이미지가 너무 길어 여기까지 캡처했습니다.";
                break;
            }
            frames.Add((next, overlap));
            totalHeight = nextHeight;
            if (page == 499) warning = "최대 스크롤 횟수에 도달했습니다.";
        }

        var height = frames.Sum(f => f.Image.PixelHeight - f.Skip);
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            var y = 0;
            foreach (var (image, skip) in frames)
            {
                var part = new CroppedBitmap(image, new Int32Rect(0, skip, image.PixelWidth, image.PixelHeight - skip));
                dc.DrawImage(part, new Rect(0, y, image.PixelWidth, image.PixelHeight - skip));
                y += image.PixelHeight - skip;
            }
        }
        var result = new RenderTargetBitmap(bounds.Width, height, 96, 96, PixelFormats.Pbgra32);
        result.Render(visual);
        result.Freeze();
        return (result, warning);
    }

    private static int FindOverlap(BitmapSource previous, BitmapSource next)
    {
        var a = new FormatConvertedBitmap(previous, PixelFormats.Bgra32, null, 0);
        var b = new FormatConvertedBitmap(next, PixelFormats.Bgra32, null, 0);
        var width = a.PixelWidth;
        var height = a.PixelHeight;
        var stride = width * 4;
        var pixelsA = new byte[stride * height];
        var pixelsB = new byte[stride * height];
        a.CopyPixels(pixelsA, stride, 0);
        b.CopyPixels(pixelsB, stride, 0);
        if (FramesMatch(previous, next)) return height;
        var xStep = Math.Max(1, width / 96);
        var yStep = Math.Max(1, height / 140);
        var minOverlap = Math.Max(24, height / 8);
        var edgeSkip = Math.Min(80, Math.Max(8, height / 12));
        var bestOverlap = -1;
        var bestScore = double.MaxValue;
        var bestMatchRatio = 0d;
        for (var overlap = height - 1; overlap >= minOverlap; overlap--)
        {
            long difference = 0;
            var matches = 0;
            var count = 0;
            var min = 255;
            var max = 0;
            var firstY = Math.Min(edgeSkip, overlap / 4);
            var lastY = overlap - firstY;
            for (var y = firstY; y < lastY; y += yStep)
            {
                var oldY = height - overlap + y;
                for (var x = 0; x < width; x += xStep)
                {
                    var ia = oldY * stride + x * 4;
                    var ib = y * stride + x * 4;
                    var pixelDifference = PixelDifference(pixelsA, pixelsB, ia, ib);
                    difference += pixelDifference;
                    if (pixelDifference <= 30) matches++;
                    min = Math.Min(min, pixelsA[ia]);
                    max = Math.Max(max, pixelsA[ia]);
                    count++;
                }
            }

            if (count < 120 || max - min < 24) continue;
            var meanDifference = difference / (double)count;
            var matchRatio = matches / (double)count;
            var score = meanDifference + (1 - matchRatio) * 30;
            if (matchRatio >= 0.55 && score < bestScore)
            {
                bestOverlap = overlap;
                bestScore = score;
                bestMatchRatio = matchRatio;
            }
        }

        return bestOverlap >= 0 && bestScore <= 24 && bestMatchRatio >= 0.55 ? bestOverlap : -1;
    }

    private static bool FramesMatch(BitmapSource previous, BitmapSource next)
    {
        var a = new FormatConvertedBitmap(previous, PixelFormats.Bgra32, null, 0);
        var b = new FormatConvertedBitmap(next, PixelFormats.Bgra32, null, 0);
        var width = a.PixelWidth; var height = a.PixelHeight; var stride = width * 4;
        var pixelsA = new byte[stride * height]; var pixelsB = new byte[stride * height];
        a.CopyPixels(pixelsA, stride, 0); b.CopyPixels(pixelsB, stride, 0);
        long difference = 0;
        var changed = 0; var total = 0;
        for (var y = 0; y < height; y += Math.Max(1, height / 120))
            for (var x = 0; x < width; x += Math.Max(1, width / 120))
            {
                var index = y * stride + x * 4;
                var pixelDifference = PixelDifference(pixelsA, pixelsB, index, index);
                difference += pixelDifference;
                if (pixelDifference > 30) changed++;
                total++;
            }
        return total > 0 && difference / (double)total <= 3 && changed <= total * 0.02;
    }

    private static int PixelDifference(byte[] a, byte[] b, int ia, int ib) =>
        Math.Abs(a[ia] - b[ib]) + Math.Abs(a[ia + 1] - b[ib + 1]) + Math.Abs(a[ia + 2] - b[ib + 2]);

    private static void ScrollWheel(uint amount) =>
        mouse_event(0x0800, 0, 0, amount, UIntPtr.Zero);

    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);
}
