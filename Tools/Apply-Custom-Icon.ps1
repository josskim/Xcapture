param(
    [string]$SourcePath = "$PSScriptRoot\..\doc\icon2.png",
    [string]$OutputDir = "$PSScriptRoot\..\Assets"
)

Add-Type -AssemblyName System.Drawing

$sourceFullPath = [System.IO.Path]::GetFullPath($SourcePath)
$outputFullDir = [System.IO.Path]::GetFullPath($OutputDir)
[System.IO.Directory]::CreateDirectory($outputFullDir) | Out-Null

function Test-CirclePixel {
    param([System.Drawing.Color]$Color)

    $isRed = $Color.R -gt 140 -and $Color.G -lt 130 -and $Color.B -lt 140
    $isCream = $Color.R -gt 190 -and $Color.G -gt 170 -and $Color.B -gt 140 -and [Math]::Abs($Color.R - $Color.G) -lt 45
    return $isRed -or $isCream
}

function New-RoundedIconPng {
    param(
        [System.Drawing.Bitmap]$Source,
        [int]$Size,
        [string]$Path
    )

    $minX = $Source.Width
    $minY = $Source.Height
    $maxX = 0
    $maxY = 0

    for ($y = 0; $y -lt $Source.Height; $y += 2) {
        for ($x = 0; $x -lt $Source.Width; $x += 2) {
            if (Test-CirclePixel -Color $Source.GetPixel($x, $y)) {
                $minX = [Math]::Min($minX, $x)
                $minY = [Math]::Min($minY, $y)
                $maxX = [Math]::Max($maxX, $x)
                $maxY = [Math]::Max($maxY, $y)
            }
        }
    }

    $centerX = ($minX + $maxX) / 2.0
    $centerY = ($minY + $maxY) / 2.0
    $diameter = [Math]::Max(($maxX - $minX), ($maxY - $minY)) + 8
    $cropX = [Math]::Max(0, [int][Math]::Round($centerX - $diameter / 2.0))
    $cropY = [Math]::Max(0, [int][Math]::Round($centerY - $diameter / 2.0))
    if ($cropX + $diameter -gt $Source.Width) { $cropX = [int]($Source.Width - $diameter) }
    if ($cropY + $diameter -gt $Source.Height) { $cropY = [int]($Source.Height - $diameter) }

    $output = New-Object System.Drawing.Bitmap $Size, $Size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($output)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $clip = New-Object System.Drawing.Drawing2D.GraphicsPath
    $clip.AddEllipse(0, 0, $Size, $Size)
    $graphics.SetClip($clip)
    $srcRect = New-Object System.Drawing.Rectangle $cropX, $cropY, ([int]$diameter), ([int]$diameter)
    $destRect = New-Object System.Drawing.Rectangle 0, 0, $Size, $Size
    $graphics.DrawImage($Source, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
    $graphics.ResetClip()

    for ($y = 0; $y -lt $output.Height; $y++) {
        for ($x = 0; $x -lt $output.Width; $x++) {
            $color = $output.GetPixel($x, $y)
            $max = [Math]::Max($color.R, [Math]::Max($color.G, $color.B))
            $min = [Math]::Min($color.R, [Math]::Min($color.G, $color.B))
            $isGrayChecker = ($max - $min) -lt 18 -and $max -lt 180
            if ($isGrayChecker) {
                $output.SetPixel($x, $y, [System.Drawing.Color]::Transparent)
            }
        }
    }

    $output.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $clip.Dispose()
    $output.Dispose()
}

function New-IcoFromPng {
    param(
        [string]$SourcePng,
        [string]$OutputIco
    )

    $source = [System.Drawing.Bitmap]::FromFile($SourcePng)
    $sizes = @(16, 32, 48, 256)
    $pngBytes = @()
    foreach ($size in $sizes) {
        $bitmap = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.DrawImage($source, 0, 0, $size, $size)
        $stream = New-Object System.IO.MemoryStream
        $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngBytes += ,$stream.ToArray()
        $stream.Dispose()
        $graphics.Dispose()
        $bitmap.Dispose()
    }
    $source.Dispose()

    $file = [System.IO.File]::Create($OutputIco)
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

$source = [System.Drawing.Bitmap]::FromFile($sourceFullPath)
$appPng = Join-Path $outputFullDir "app.png"
$appIco = Join-Path $outputFullDir "app.ico"
New-RoundedIconPng -Source $source -Size 512 -Path $appPng
$source.Dispose()
New-IcoFromPng -SourcePng $appPng -OutputIco $appIco

Write-Host "Updated app icon:"
Write-Host $appPng
Write-Host $appIco
