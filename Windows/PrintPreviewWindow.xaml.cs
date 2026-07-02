using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ComboBox = System.Windows.Controls.ComboBox;
using Size = System.Windows.Size;

namespace XCapture.Windows;

public partial class PrintPreviewWindow : Window
{
    private const double PreviewMaxWidth = 520;
    private const double PreviewMaxHeight = 560;
    private const double PrintMargin = 48;

    private readonly BitmapSource _image;

    public PrintPreviewWindow(BitmapSource image)
    {
        InitializeComponent();
        _image = image;
        PreviewImage.Source = image;
        UpdatePreview();
    }

    private void PrintOption_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        UpdatePreview();
    }

    private void PrintButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Controls.PrintDialog
        {
            PrintTicket = new PrintTicket
            {
                PageOrientation = GetOrientation(),
                PageMediaSize = GetPageMediaSize()
            }
        };

        if (dialog.ShowDialog() != true) return;

        var pageSize = new Size(
            Math.Max(1, dialog.PrintableAreaWidth),
            Math.Max(1, dialog.PrintableAreaHeight));
        var visual = CreatePrintVisual(pageSize);
        dialog.PrintVisual(visual, "XCapture");
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void UpdatePreview()
    {
        var paper = GetPaperSize();
        var previewScale = Math.Min(PreviewMaxWidth / paper.Width, PreviewMaxHeight / paper.Height);
        var previewPaper = new Size(paper.Width * previewScale, paper.Height * previewScale);
        var printable = GetPrintableRect(previewPaper);
        var imageRect = GetImageRect(printable, previewScale);

        PaperBorder.Width = previewPaper.Width;
        PaperBorder.Height = previewPaper.Height;
        PaperContent.Width = previewPaper.Width;
        PaperContent.Height = previewPaper.Height;

        PreviewImage.Width = imageRect.Width;
        PreviewImage.Height = imageRect.Height;
        PreviewImage.Margin = new Thickness(imageRect.Left, imageRect.Top, 0, 0);
        PreviewImage.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
        PreviewImage.VerticalAlignment = VerticalAlignment.Top;

        PreviewInfoText.Text = $"{GetOrientationLabel()} / {GetPaperLabel()} / {GetScaleLabel()}";
    }

    private DrawingVisual CreatePrintVisual(Size pageSize)
    {
        var visual = new DrawingVisual();
        using var context = visual.RenderOpen();

        var page = new Rect(0, 0, pageSize.Width, pageSize.Height);
        context.DrawRectangle(System.Windows.Media.Brushes.White, null, page);
        context.DrawImage(_image, GetImageRect(GetPrintableRect(pageSize), 1));

        return visual;
    }

    private Rect GetPrintableRect(Size paperSize)
    {
        var margin = Math.Min(PrintMargin, Math.Min(paperSize.Width, paperSize.Height) / 8);
        return new Rect(
            margin,
            margin,
            Math.Max(1, paperSize.Width - margin * 2),
            Math.Max(1, paperSize.Height - margin * 2));
    }

    private Rect GetImageRect(Rect printable, double scale)
    {
        var dpiScaleX = Math.Max(0.1, _image.DpiX / 96.0);
        var dpiScaleY = Math.Max(0.1, _image.DpiY / 96.0);
        var imageWidth = _image.PixelWidth / dpiScaleX * scale;
        var imageHeight = _image.PixelHeight / dpiScaleY * scale;

        if (GetScaleMode() == "Fit")
        {
            var fitScale = Math.Min(printable.Width / imageWidth, printable.Height / imageHeight);
            imageWidth *= fitScale;
            imageHeight *= fitScale;
        }

        return new Rect(
            printable.Left + (printable.Width - imageWidth) / 2,
            printable.Top + (printable.Height - imageHeight) / 2,
            imageWidth,
            imageHeight);
    }

    private Size GetPaperSize()
    {
        var a4 = new Size(793.7, 1122.5);
        var letter = new Size(816, 1056);
        var size = GetPaperCode() == "Letter" ? letter : a4;

        return GetOrientation() == PageOrientation.Landscape
            ? new Size(size.Height, size.Width)
            : size;
    }

    private PageOrientation GetOrientation()
    {
        return GetSelectedTag(OrientationBox) == "Landscape"
            ? PageOrientation.Landscape
            : PageOrientation.Portrait;
    }

    private PageMediaSize GetPageMediaSize()
    {
        return GetPaperCode() == "Letter"
            ? new PageMediaSize(PageMediaSizeName.NorthAmericaLetter)
            : new PageMediaSize(PageMediaSizeName.ISOA4);
    }

    private string GetPaperCode() => GetSelectedTag(PaperSizeBox) == "Letter" ? "Letter" : "A4";
    private string GetScaleMode() => GetSelectedTag(ScaleBox) == "Actual" ? "Actual" : "Fit";

    private string GetOrientationLabel() => GetOrientation() == PageOrientation.Landscape ? "가로" : "세로";
    private string GetPaperLabel() => GetPaperCode() == "Letter" ? "Letter" : "A4";
    private string GetScaleLabel() => GetScaleMode() == "Actual" ? "원본 크기" : "페이지에 맞추기";

    private static string? GetSelectedTag(ComboBox comboBox)
    {
        return (comboBox.SelectedItem as ComboBoxItem)?.Tag as string;
    }
}
