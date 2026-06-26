namespace PowerOffScreensaver.Services;

/// <summary>
/// Detects whether the interactive session is currently locked (secure desktop
/// is the input desktop). Used by <see cref="PowerOffScreensaver.LockGuard"/> to
/// verify a lock request actually took effect.
/// </summary>
public interface ILockStateProbe
{
    bool IsLocked();
}
