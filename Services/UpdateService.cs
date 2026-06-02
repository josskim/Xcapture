using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace XCapture.Services;

public static class UpdateService
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/josskim/Xcapture/releases/latest";
    private static readonly HttpClient Http = new();
    private static bool _hasUserAgent;

    public static async Task CheckForUpdatesAsync(Window owner, bool showUpToDateMessage = false)
    {
        try
        {
            EnsureUserAgent();
            using var response = await Http.GetAsync(LatestReleaseUrl);
            if (!response.IsSuccessStatusCode)
            {
                ShowManualMessage(owner, showUpToDateMessage, "업데이트 정보를 확인할 수 없습니다.");
                return;
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(stream);
            var root = document.RootElement;

            var tagName = root.GetProperty("tag_name").GetString() ?? "";
            var latestVersionText = tagName.Trim().TrimStart('v', 'V');
            if (!Version.TryParse(latestVersionText, out var latestVersion))
            {
                ShowManualMessage(owner, showUpToDateMessage, "최신 버전 정보를 읽을 수 없습니다.");
                return;
            }

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
            if (latestVersion <= currentVersion)
            {
                ShowManualMessage(owner, showUpToDateMessage, $"현재 최신 버전입니다.\n현재 버전: {currentVersion}");
                return;
            }

            var downloadUrl = FindInstallerDownloadUrl(root);
            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                ShowManualMessage(owner, showUpToDateMessage, $"새 버전 {latestVersion}이 있지만 설치 파일을 찾을 수 없습니다.");
                return;
            }

            var answer = MessageBox.Show(
                owner,
                $"새 버전 {latestVersion} 업데이트가 있습니다.\n현재 버전: {currentVersion}\n\n업데이트 하시겠습니까?",
                "XCapture 업데이트",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            var installerPath = await DownloadInstallerAsync(downloadUrl, latestVersion);
            Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true
            });

            Application.Current.Shutdown();
        }
        catch (Exception exc)
        {
            LogService.Error(exc, "Update check failed");
            ShowManualMessage(owner, showUpToDateMessage, $"업데이트 확인 중 오류가 발생했습니다.\n\n로그: {LogService.LogPath}");
        }
    }

    private static void EnsureUserAgent()
    {
        if (_hasUserAgent)
        {
            return;
        }

        Http.DefaultRequestHeaders.UserAgent.ParseAdd("XCapture");
        _hasUserAgent = true;
    }

    private static void ShowManualMessage(Window owner, bool showMessage, string message)
    {
        if (!showMessage)
        {
            return;
        }

        MessageBox.Show(owner, message, "XCapture 업데이트", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string? FindInstallerDownloadUrl(JsonElement releaseRoot)
    {
        if (!releaseRoot.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? "";
            if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!name.Contains("XCaptureSetup", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return asset.GetProperty("browser_download_url").GetString();
        }

        return null;
    }

    private static async Task<string> DownloadInstallerAsync(string downloadUrl, Version version)
    {
        var updateDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "XCapture",
            "Updates");
        Directory.CreateDirectory(updateDir);

        var installerPath = Path.Combine(updateDir, $"XCaptureSetup-{version}.exe");
        using var response = await Http.GetAsync(downloadUrl);
        response.EnsureSuccessStatusCode();
        await using var source = await response.Content.ReadAsStreamAsync();
        await using var destination = File.Create(installerPath);
        await source.CopyToAsync(destination);
        return installerPath;
    }
}
