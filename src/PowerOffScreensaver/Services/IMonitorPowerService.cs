namespace PowerOffScreensaver.Services;

public interface IMonitorPowerService
{
    /// <summary>Power monitors off (DPMS) for burn-in / energy protection.</summary>
    void TryPowerOff();

    /// <summary>Power monitors back on so the lock screen is actually visible.</summary>
    void TryPowerOn();
}
