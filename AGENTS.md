# Agent Rules

## XCapture Release Rule

When XCapture is changed, do not stop at code changes.

Always complete the release/update path unless the user explicitly says to skip it:

1. Build and verify the change.
2. Bump the version in `XCapture.csproj` and `Installer/xcapture.iss`.
3. Rebuild `Installer/XCaptureSetup.exe`.
4. Commit and push the code changes to GitHub.
5. Push the matching Git tag.
6. Create or update the matching GitHub Release.
7. Upload the latest `XCaptureSetup.exe` as the Release asset.
8. Confirm `https://api.github.com/repos/josskim/Xcapture/releases/latest` returns the new version and `XCaptureSetup.exe`.
