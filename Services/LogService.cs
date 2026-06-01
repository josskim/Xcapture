using System.IO;
using System.Text;

namespace XCapture.Services;

public static class LogService
{
    private static readonly object Sync = new();
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "XCapture");

    public static string LogPath => Path.Combine(LogDirectory, "xcapture.log");

    public static void Error(Exception exception, string context)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            var text = new StringBuilder()
                .AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}")
                .AppendLine(exception.ToString())
                .AppendLine()
                .ToString();

            lock (Sync)
            {
                File.AppendAllText(LogPath, text, Encoding.UTF8);
            }
        }
        catch
        {
            // Logging must never crash the app.
        }
    }
}
