namespace PowerOffScreensaver.Services;

public interface IDdcCiService
{
    bool IsSupported { get; }
    void TryPowerOff();
}
