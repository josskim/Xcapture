using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace XCapture.Services;

public static class ScrollCaptureService
{
    public static async Task<(BitmapSource Image, string? Warning)> CaptureAsync(Rectangle bounds, Rectangle selected)
    {
        SetCursorPos(selected.Left + selected.Width / 2, selected.Top + selected.Height / 2);
        var before = ScreenCaptureService.CaptureRectangle(bounds);
        var reachedTop = false;
        for (var attempt = 0; attempt < 100; attempt++)
        {
            mouse_event(0x0800, 0, 0, 720, UIntPtr.Zero);
            await Task.Delay(250);
            var after = ScreenCaptureService.CaptureRectangle(bounds);
            if (FramesMatch(before, after)) { reachedTop = true; break; }
            before = after;
        }
        if (!reachedTop) throw new InvalidOperationException("스크롤 화면의 시작점을 찾지 못했습니다. 페이지를 맨 위로 이동한 뒤 다시 시도해주세요.");
        var frames = new List<(BitmapSource Image, int Skip)> { (ScreenCaptureService.CaptureRectangle(bounds), 0) };
        string? warning = null;
        for (var page = 0; page < 200; page++)
        {
            mouse_event(0x0800, 0, 0, unchecked((uint)-120), UIntPtr.Zero);
            await Task.Delay(600);
            var next = ScreenCaptureService.CaptureRectangle(bounds);
            var overlap = FindOverlap(frames[^1].Image, next);
            if (overlap == bounds.Height)
            {
                if (frames.Count == 1) warning = "스크롤로 화면이 이동하지 않았습니다. 스크롤 가능한 내용 영역을 선택했는지 확인해주세요.";
                break;
            }
            if (overlap < 0)
            {
                warning = "이미지 겹치는 부분을 확인할 수 없어 여기까지 캡처했습니다. 고정 메뉴나 스크롤바를 제외하고 다시 선택해 보세요.";
                break;
            }
            var nextHeight = frames.Sum(f => f.Image.PixelHeight - f.Skip) + next.PixelHeight - overlap;
            if (nextHeight > 30000 || (long)bounds.Width * nextHeight > 100_000_000)
            {
                warning = "이미지가 너무 길어 여기까지 캡처했습니다.";
                break;
            }
            frames.Add((next, overlap));
            if (page == 199) warning = "최대 스크롤 횟수에 도달했습니다.";
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
        var columnMoving = new bool[width];
        var rowMoving = new bool[height];
        var xStep = Math.Max(1, width / 80);
        var yStep = Math.Max(1, height / 100);
        for (var x = 0; x < width; x += xStep)
        {
            var changed = 0;
            for (var y = 0; y < height; y += yStep)
                if (PixelDifference(pixelsA, pixelsB, y * stride + x * 4, y * stride + x * 4) > 15) changed++;
            columnMoving[x] = changed >= 2;
        }
        for (var y = 0; y < height; y++)
        {
            var changed = 0;
            for (var x = 0; x < width; x += xStep)
                if (PixelDifference(pixelsA, pixelsB, y * stride + x * 4, y * stride + x * 4) > 15) changed++;
            rowMoving[y] = changed >= 2;
        }
        for (var overlap = height - 1; overlap >= Math.Max(16, height / 10); overlap--)
        {
            long difference = 0;
            var count = 0;
            var min = 255;
            var max = 0;
            for (var y = 0; y < overlap; y += Math.Max(1, overlap / 80))
            {
                var oldY = height - overlap + y;
                if (!rowMoving[oldY] || !rowMoving[y]) continue;
                for (var x = 0; x < width; x += xStep)
                {
                    if (!columnMoving[x]) continue;
                    var ia = oldY * stride + x * 4;
                    var ib = y * stride + x * 4;
                    difference += PixelDifference(pixelsA, pixelsB, ia, ib);
                    min = Math.Min(min, pixelsA[ia]);
                    max = Math.Max(max, pixelsA[ia]);
                    count += 3;
                }
            }
            if (count >= 90 && difference / (double)count < 2 && max - min >= 24) return overlap;
        }
        return -1;
    }

    private static bool FramesMatch(BitmapSource previous, BitmapSource next)
    {
        var a = new FormatConvertedBitmap(previous, PixelFormats.Bgra32, null, 0);
        var b = new FormatConvertedBitmap(next, PixelFormats.Bgra32, null, 0);
        var width = a.PixelWidth; var height = a.PixelHeight; var stride = width * 4;
        var pixelsA = new byte[stride * height]; var pixelsB = new byte[stride * height];
        a.CopyPixels(pixelsA, stride, 0); b.CopyPixels(pixelsB, stride, 0);
        var changed = 0; var total = 0;
        for (var y = 0; y < height; y += Math.Max(1, height / 100))
            for (var x = 0; x < width; x += Math.Max(1, width / 100))
            {
                var index = y * stride + x * 4;
                if (PixelDifference(pixelsA, pixelsB, index, index) > 15) changed++;
                total++;
            }
        return changed == 0;
    }

    private static int PixelDifference(byte[] a, byte[] b, int ia, int ib) =>
        Math.Abs(a[ia] - b[ib]) + Math.Abs(a[ia + 1] - b[ib + 1]) + Math.Abs(a[ia + 2] - b[ib + 2]);

    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);
}
