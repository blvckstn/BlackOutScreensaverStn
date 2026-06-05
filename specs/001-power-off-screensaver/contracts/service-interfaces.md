# Contract: Service Interfaces

All Win32 P/Invoke and external I/O is encapsulated behind these interfaces (Constitution Principle III).

---

## `IMonitorPowerService`

Sends the OS-level monitor power-off signal.

```csharp
namespace PowerOffScreensaver.Services;

public interface IMonitorPowerService
{
    /// <summary>
    /// Sends WM_SYSCOMMAND SC_MONITORPOWER to HWND_BROADCAST.
    /// Never throws — failure is silently ignored.
    /// </summary>
    void TryPowerOff();
}
```

**Contract**:
- `TryPowerOff()` MUST NOT throw under any circumstances.
- May be called from any thread; implementation must marshal to appropriate context if needed.
- Has no return value — caller cannot rely on success.

---

## `IWorkstationLockService`

Locks the interactive Windows session.

```csharp
namespace PowerOffScreensaver.Services;

public interface IWorkstationLockService
{
    /// <summary>
    /// Calls LockWorkStation(). Swallows failure in non-interactive sessions.
    /// </summary>
    void TryLock();
}
```

**Contract**:
- `TryLock()` MUST NOT throw.
- Called at most once per screensaver session (enforced by `ScreensaverHost`).

---

## `IDdcCiService`

Optional DDC/CI monitor power-off. Default implementation is a no-op stub.

```csharp
namespace PowerOffScreensaver.Services;

public interface IDdcCiService
{
    /// <summary>
    /// True if DDC/CI is supported on at least one connected monitor.
    /// Always false on NullDdcCiService.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Attempts to send VCP Feature 0xD6 (Display Power Mode = Off) to all
    /// DDC/CI-capable monitors. Swallows all exceptions.
    /// </summary>
    void TryPowerOff();
}
```

**Contract**:
- `IsSupported` MUST be safe to call any number of times without side effects.
- `TryPowerOff()` MUST NOT throw.
- Implementations MUST NOT block the UI thread for more than 200 ms.
- `NullDdcCiService` (default) always returns `IsSupported = false` and `TryPowerOff()` is a pure no-op.

---

## `ISettingsService`

Reads and writes `AppSettings` to disk.

```csharp
namespace PowerOffScreensaver.Services;

public interface ISettingsService
{
    AppSettings Load();
    void Save(AppSettings settings);
}
```

**Contract**:
- `Load()` MUST return a valid `AppSettings` with defaults if file is absent or corrupt.
- `Load()` MUST NOT throw.
- `Save()` MAY throw `IOException` — caller handles UI-level error display.
- Thread safety: not required (called on UI thread only).

---

## `BlackoutForm` — Exit Event

`BlackoutForm` raises an event (not a formal interface) consumed by `ScreensaverHost`:

```csharp
public event EventHandler? ExitRequested;
```

**Contract**:
- Raised at most once per `BlackoutForm` instance.
- Always raised on the UI thread.
- `ScreensaverHost` subscribes before calling `Show()`.
- After `ExitRequested` is raised the form may be closed by the host at any time.
