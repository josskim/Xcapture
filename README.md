# XCapture

Simple Windows screen capture tool.

## Features

- Region capture: `Ctrl+Shift+S`
- Full screen capture: `Ctrl+Shift+A`
- Main window on app launch
- Recent capture history: latest 10 images
- Auto-copy captured image to clipboard
- Pen and eraser markup
- PNG save
- Print
- System tray menu

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

## GitHub Release Auto Update

XCapture checks the latest GitHub Release on startup:

```text
https://github.com/josskim/Xcapture/releases/latest
```

To publish an update:

1. Increase `Version` in `XCapture.csproj`.
2. Increase `MyAppVersion` in `Installer/xcapture.iss`.
3. Build `Installer/XCaptureSetup.exe`.
4. Create a GitHub Release tag such as `v0.2.4`.
5. Attach `XCaptureSetup.exe` to the release.

When an installed app starts and finds a newer release asset named `XCaptureSetup*.exe`, it asks the user whether to update, downloads the installer, runs it, and exits.
