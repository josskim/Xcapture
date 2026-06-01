param(
    [string]$OutputDir = "$PSScriptRoot\..\Assets"
)

Add-Type -AssemblyName System.Drawing

$resolvedOutput = [System.IO.Path]::GetFullPath($OutputDir)
[System.IO.Directory]::CreateDirectory($resolvedOutput) | Out-Null

function New-IconBitmap {
    param(
        [int]$Size,
        [string]$Background,
        [string]$Accent,
        [string]$Name
    )

    $bitmap = New-Object System.Drawing.Bitmap $Size, $Size
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $bgRect = New-Object System.Drawing.RectangleF 0, 0, $Size, $Size
    $bgPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $radius = [Math]::Round($Size * 0.22)
    $diameter = $radius * 2
    $bgPath.AddArc($bgRect.X, $bgRect.Y, $diameter, $diameter, 180, 90)
    $bgPath.AddArc($bgRect.Right - $diameter, $bgRect.Y, $diameter, $diameter, 270, 90)
    $bgPath.AddArc($bgRect.Right - $diameter, $bgRect.Bottom - $diameter, $diameter, $diameter, 0, 90)
    $bgPath.AddArc($bgRect.X, $bgRect.Bottom - $diameter, $diameter, $diameter, 90, 90)
    $bgPath.CloseFigure()

    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $bgRect, ([System.Drawing.ColorTranslator]::FromHtml($Background)), ([System.Drawing.ColorTranslator]::FromHtml("#0F172A")), 45
    $graphics.FillPath($brush, $bgPath)

    $accentColor = [System.Drawing.ColorTranslator]::FromHtml($Accent)
    $white = [System.Drawing.Color]::FromArgb(245, 255, 255, 255)
    $muted = [System.Drawing.Color]::FromArgb(130, 255, 255, 255)

    $framePen = New-Object System.Drawing.Pen $white, ([Math]::Max(3, $Size * 0.045))
    $accentPen = New-Object System.Drawing.Pen $accentColor, ([Math]::Max(4, $Size * 0.055))
    $mutedPen = New-Object System.Drawing.Pen $muted, ([Math]::Max(2, $Size * 0.02))
    $framePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $framePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $accentPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $accentPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

    $pad = $Size * 0.2
    $frame = New-Object System.Drawing.RectangleF $pad, ($Size * 0.23), ($Size - $pad * 2), ($Size * 0.52)
    $graphics.DrawRectangle($framePen, $frame.X, $frame.Y, $frame.Width, $frame.Height)

    switch ($Name) {
        "frame-x" {
            $graphics.DrawLine($accentPen, $Size * 0.35, $Size * 0.36, $Size * 0.65, $Size * 0.64)
            $graphics.DrawLine($accentPen, $Size * 0.65, $Size * 0.36, $Size * 0.35, $Size * 0.64)
        }
        "crop-bolt" {
            $graphics.DrawLine($mutedPen, $Size * 0.26, $Size * 0.48, $Size * 0.74, $Size * 0.48)
            $graphics.DrawLine($mutedPen, $Size * 0.50, $Size * 0.28, $Size * 0.50, $Size * 0.72)
            $points = @(
                [System.Drawing.PointF]::new($Size * 0.53, $Size * 0.31),
                [System.Drawing.PointF]::new($Size * 0.39, $Size * 0.54),
                [System.Drawing.PointF]::new($Size * 0.53, $Size * 0.52),
                [System.Drawing.PointF]::new($Size * 0.46, $Size * 0.71),
                [System.Drawing.PointF]::new($Size * 0.66, $Size * 0.44),
                [System.Drawing.PointF]::new($Size * 0.51, $Size * 0.47)
            )
            $graphics.FillPolygon((New-Object System.Drawing.SolidBrush $accentColor), $points)
        }
        "lens-pen" {
            $graphics.DrawEllipse($accentPen, $Size * 0.34, $Size * 0.32, $Size * 0.28, $Size * 0.28)
            $graphics.DrawLine($accentPen, $Size * 0.58, $Size * 0.58, $Size * 0.71, $Size * 0.71)
            $graphics.DrawLine($framePen, $Size * 0.31, $Size * 0.73, $Size * 0.68, $Size * 0.32)
        }
    }

    $graphics.Dispose()
    return $bitmap
}

function Save-Png {
    param([System.Drawing.Bitmap]$Bitmap, [string]$Path)
    $Bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
}

function New-Ico {
    param(
        [string]$Path,
        [string]$Background,
        [string]$Accent,
        [string]$Name
    )

    $sizes = @(16, 32, 48, 256)
    $pngBytes = @()
    foreach ($size in $sizes) {
        $bitmap = New-IconBitmap -Size $size -Background $Background -Accent $Accent -Name $Name
        $stream = New-Object System.IO.MemoryStream
        $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngBytes += ,$stream.ToArray()
        $stream.Dispose()
        $bitmap.Dispose()
    }

    $file = [System.IO.File]::Create($Path)
    $writer = New-Object System.IO.BinaryWriter $file
    $writer.Write([UInt16]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]$sizes.Count)

    $offset = 6 + (16 * $sizes.Count)
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $size = $sizes[$i]
        $bytes = $pngBytes[$i]
        $writer.Write([byte]($(if ($size -eq 256) { 0 } else { $size })))
        $writer.Write([byte]($(if ($size -eq 256) { 0 } else { $size })))
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]32)
        $writer.Write([UInt32]$bytes.Length)
        $writer.Write([UInt32]$offset)
        $offset += $bytes.Length
    }

    foreach ($bytes in $pngBytes) {
        $writer.Write($bytes)
    }

    $writer.Dispose()
    $file.Dispose()
}

$variants = @(
    @{ Name = "frame-x"; Background = "#2563EB"; Accent = "#22D3EE"; File = "icon-frame-x" },
    @{ Name = "crop-bolt"; Background = "#111827"; Accent = "#FACC15"; File = "icon-crop-bolt" },
    @{ Name = "lens-pen"; Background = "#059669"; Accent = "#A7F3D0"; File = "icon-lens-pen" }
)

foreach ($variant in $variants) {
    $bitmap = New-IconBitmap -Size 256 -Background $variant.Background -Accent $variant.Accent -Name $variant.Name
    Save-Png -Bitmap $bitmap -Path (Join-Path $resolvedOutput "$($variant.File).png")
    $bitmap.Dispose()
    New-Ico -Path (Join-Path $resolvedOutput "$($variant.File).ico") -Background $variant.Background -Accent $variant.Accent -Name $variant.Name
}

Copy-Item (Join-Path $resolvedOutput "icon-frame-x.ico") (Join-Path $resolvedOutput "app.ico") -Force
Copy-Item (Join-Path $resolvedOutput "icon-frame-x.png") (Join-Path $resolvedOutput "app.png") -Force

Write-Host "Generated icons in $resolvedOutput"
