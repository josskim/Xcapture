using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace XCapture.Services;

public static class ImageOperations
{
    public static BitmapSource MergeVertical(IReadOnlyList<BitmapSource> images)
    {
        if (images.Count == 0) throw new ArgumentException("이미지를 선택해주세요.");
        var width = images.Max(x => x.PixelWidth);
        var height = checked(images.Sum(x => x.PixelHeight));
        if ((long)width * height > 100_000_000) throw new InvalidOperationException("합친 이미지가 너무 큽니다. PDF로 저장하거나 이미지 수를 줄여주세요.");
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(System.Windows.Media.Brushes.White, null, new Rect(0, 0, width, height));
            var y = 0;
            foreach (var image in images)
            {
                dc.DrawImage(image, new Rect(0, y, image.PixelWidth, image.PixelHeight));
                y += image.PixelHeight;
            }
        }
        var result = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        result.Render(visual);
        result.Freeze();
        return result;
    }

    public static BitmapSource Resize(BitmapSource image, int width, int height)
    {
        if (width < 1 || height < 1 || (long)width * height > 100_000_000) throw new ArgumentException("이미지 크기를 확인해주세요.");
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen()) dc.DrawImage(image, new Rect(0, 0, width, height));
        var result = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        result.Render(visual);
        result.Freeze();
        return result;
    }

    public static void Save(BitmapSource image, string path, int quality)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        BitmapEncoder encoder = extension switch
        {
            ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = Math.Clamp(quality, 1, 100) },
            ".png" => new PngBitmapEncoder(),
            ".bmp" => new BmpBitmapEncoder(),
            _ => throw new ArgumentException("지원하지 않는 파일 형식입니다.")
        };
        encoder.Frames.Add(BitmapFrame.Create(extension is ".jpg" or ".jpeg" ? Flatten(image) : image));
        using var file = File.Create(path);
        encoder.Save(file);
    }

    public static void SavePdf(IReadOnlyList<BitmapSource> images, string path, int quality = 95)
    {
        using var file = File.Create(path);
        var offsets = new List<long> { 0 };
        void Write(string text) { var bytes = Encoding.ASCII.GetBytes(text); file.Write(bytes); }
        void Start(int id) { offsets.Add(file.Position); Write($"{id} 0 obj\n"); }
        Write("%PDF-1.4\n%XCapture\n");
        Start(1); Write("<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        Start(2); Write($"<< /Type /Pages /Count {images.Count} /Kids [{string.Join(" ", Enumerable.Range(0, images.Count).Select(i => $"{3 + i * 3} 0 R"))}] >>\nendobj\n");
        for (var i = 0; i < images.Count; i++)
        {
            var image = images[i];
            var id = 3 + i * 3;
            var width = image.PixelWidth;
            var height = image.PixelHeight;
            using var jpeg = new MemoryStream();
            var encoder = new JpegBitmapEncoder { QualityLevel = quality };
            encoder.Frames.Add(BitmapFrame.Create(Flatten(image)));
            encoder.Save(jpeg);
            Start(id); Write($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {width} {height}] /Resources << /XObject << /Im0 {id + 1} 0 R >> >> /Contents {id + 2} 0 R >>\nendobj\n");
            Start(id + 1); Write($"<< /Type /XObject /Subtype /Image /Width {width} /Height {height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {jpeg.Length} >>\nstream\n");
            jpeg.Position = 0; jpeg.CopyTo(file); Write("\nendstream\nendobj\n");
            var content = $"q\n{width} 0 0 {height} 0 0 cm\n/Im0 Do\nQ\n";
            Start(id + 2); Write($"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream\nendobj\n");
        }
        var xref = file.Position;
        Write($"xref\n0 {offsets.Count}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) Write($"{offset:0000000000} 00000 n \n");
        Write($"trailer\n<< /Size {offsets.Count} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
    }

    private static BitmapSource Flatten(BitmapSource image)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(System.Windows.Media.Brushes.White, null, new Rect(0, 0, image.PixelWidth, image.PixelHeight));
            dc.DrawImage(image, new Rect(0, 0, image.PixelWidth, image.PixelHeight));
        }
        var rendered = new RenderTargetBitmap(image.PixelWidth, image.PixelHeight, 96, 96, PixelFormats.Pbgra32);
        rendered.Render(visual);
        return new FormatConvertedBitmap(rendered, PixelFormats.Bgr24, null, 0);
    }
}
