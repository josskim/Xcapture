# XCapture

Simple Windows screen capture tool.

## Features

- Region capture: `Ctrl+Shift+S` by default
- Region capture freezes the screen first, preserving open browser menus and popups
- Full screen capture: `Ctrl+Shift+A` by default
- Customizable global capture shortcuts
- Main window on app launch
- Recent capture history: latest 20 images
- Auto-copy captured image to clipboard
- Pen and eraser markup
- PNG save
- Print
- System tray menu
- Image viewer for PNG, JPEG, BMP, GIF and TIFF files
- Fit-to-window, actual-size and zoom controls
- Previous/next navigation across images in the same folder
- File drag-and-drop and direct editing from the viewer
- Installer registration in Windows Open with and image right-click menus

## Run

```powershell
cd E:\dev\xcapture
dotnet run
```

## Publish

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o E:\dev\xcapture\publish-latest
```

## Build Installer

```powershell
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" .\Installer\xcapture.iss
```

Output:

```text
E:\dev\xcapture\Installer\XCaptureSetup.exe
```

## Update Existing PCs

Keep the same `AppId` in `Installer\xcapture.iss`, increase `MyAppVersion`, rebuild `XCaptureSetup.exe`, and run the new installer on the target PC. Inno Setup will update the existing installation in place.

The installer includes an optional startup shortcut task. If selected, XCapture runs automatically when Windows starts.
