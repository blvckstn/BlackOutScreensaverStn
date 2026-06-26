namespace PowerOffScreensaver.Services;

public interface IWorkstationLockService
{
    /// <summary>Requests a lock via the primary path. Returns the OS success flag.</summary>
    bool TryLock();

    /// <summary>Independent last-resort lock via a separate process / code path.</summary>
    bool TryLockFallback();
}
