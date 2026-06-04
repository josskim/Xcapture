using System.IO;
using System.Media;
using System.Windows.Media;

namespace XCapture.Services;

public static class CaptureSoundService
{
    private static readonly object LockObject = new();
    private static MediaPlayer? _player;

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

            lock (LockObject)
            {
                _player?.Close();
                _player = new MediaPlayer
                {
                    Volume = 1.0
                };
                _player.MediaEnded += (_, _) => ClosePlayer();
                _player.MediaFailed += (_, args) =>
                {
                    ClosePlayer();
                    LogService.Error(args.ErrorException, "Failed to play capture sound file");
                    SystemSounds.Asterisk.Play();
                };
                _player.Open(new Uri(soundPath, UriKind.Absolute));
                _player.Play();
            }
        }
        catch (Exception exc)
        {
            LogService.Error(exc, "Failed to play capture sound");
            SystemSounds.Asterisk.Play();
        }
    }

    private static void ClosePlayer()
    {
        lock (LockObject)
        {
            _player?.Close();
            _player = null;
        }
    }
}
