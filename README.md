# XCapture

Simple Windows screen capture tool.

## Features

- Region capture: `Ctrl+Shift+S` by default
- Region capture freezes the screen first, preserving open browser menus and popups
- Full screen capture: `Ctrl+Shift+A` by default
- Scroll capture: `Ctrl+Shift+D` by default; select a Chrome or Edge page and XCapture captures the full visible page width from top to bottom
- Customizable global capture shortcuts
- Main window on app launch
- Recent capture history: latest 50 images
- Auto-copy captured image to clipboard
- Capture results and regular images open in the same XViewer window
- XViewer opens capture images with the pen tool selected; pen and eraser markup with hover-readable toolbar buttons
- Prompts before discarding unsaved edits
- JPEG save by default, with PNG option
- Print
- System tray menu
- XViewer for PNG, JPEG, BMP, GIF and TIFF files
- Combine selected images vertically without resizing or save as a multipage PDF; preview and change the order before saving
- Resize and convert images to JPEG, PNG or BMP; control JPEG quality
- Fit-to-window, actual-size and zoom controls
- Previous/next navigation across images in the same folder
- File drag-and-drop and direct editing from the viewer
- Installer registration in Windows Open with and image right-click menus

## Run

```powershell
cd D:\dev\xcapture
dotnet run
```

## Publish

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o D:\dev\xcapture\publish-latest
```

## Build Installer

```powershell
& "D:\dev\.tools\Inno\ISCC.exe" .\Installer\xcapture.iss
```

Output:

```text
D:\dev\xcapture\Installer\XCaptureSetup.exe
```

## Update Existing PCs

Keep the same `AppId` in `Installer\xcapture.iss`, increase `MyAppVersion`, rebuild `XCaptureSetup.exe`, and run the new installer on the target PC. Inno Setup will update the existing installation in place.

The installer includes an optional startup shortcut task. If selected, XCapture runs automatically when Windows starts.
