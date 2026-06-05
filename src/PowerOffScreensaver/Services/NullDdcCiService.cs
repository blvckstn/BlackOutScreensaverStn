namespace PowerOffScreensaver.Services;

public class NullDdcCiService : IDdcCiService
{
    public bool IsSupported => false;

    public void TryPowerOff()
    {
        // No-op stub
    }
}
