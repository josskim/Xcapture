using System.Media;

namespace XCapture.Services;

public static class CaptureSoundService
{
    public static void PlayShutter()
    {
        try
        {
            SystemSounds.Asterisk.Play();
        }
        catch (Exception exc)
        {
            LogService.Error(exc, "Failed to play capture sound");
        }
    }
}
