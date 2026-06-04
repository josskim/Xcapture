using System.IO;
using System.Media;
using System.Windows.Media;

namespace XCapture.Services;

public static class CaptureSoundService
{
    public static void PlayShutter()
    {
        try
        {
            var soundPath = Path.Combine(AppContext.BaseDirectory, "Assets", "camera.mp3");
            if (!File.Exists(soundPath))
            {
                SystemSounds.Asterisk.Play();
                return;
            }

            var player = new MediaPlayer();
            player.Open(new Uri(soundPath, UriKind.Absolute));
            player.MediaEnded += (_, _) => player.Close();
            player.MediaFailed += (_, args) =>
            {
                player.Close();
                LogService.Error(args.ErrorException, "Failed to play capture sound file");
                SystemSounds.Asterisk.Play();
            };
            player.Play();
        }
        catch (Exception exc)
        {
            LogService.Error(exc, "Failed to play capture sound");
            SystemSounds.Asterisk.Play();
        }
    }
}
