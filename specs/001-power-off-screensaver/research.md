# Research: PowerOffScreensaver

**Phase 0 output** — resolves all NEEDS CLARIFICATION from Technical Context.

---

## 1. Windows Screensaver Protocol

**Decision**: Implement standard `.scr` argument protocol — `/s`, `/c[:<HWND>]`, `/p <HWND>`.

**Rationale**: Windows Shell launches the `.scr` binary with these exact arguments. Without them the OS won't manage the screensaver lifecycle (timeout, password-lock, preview) correctly. The binary must accept them at startup *before* creating any windows.

**How it works**:

| Argument | Meaning | Required behaviour |
|---|---|---|
| `/s` | Activate screensaver | Full run — black windows + power off |
| `/c` or `/c:HWND` | Open settings | Show `SettingsForm`; HWND ignored in v1 |
| `/p HWND` | Preview | No-op exit (return 0) |
| *(none)* | Direct launch | Treat as `/c` |

**Reference**: [Microsoft docs — Writing a Screensaver](https://learn.microsoft.com/en-us/windows/win32/winmsg/using-messages-and-message-queues#implementing-a-window-procedure)

---

## 2. Multi-Monitor Enumeration

**Decision**: `Screen.AllScreens` (WinForms) for MVP.

**Rationale**: `Screen.AllScreens` returns a `Screen[]` with `Bounds` (Rectangle in screen coordinates) for every connected display. It is the simplest approach and sufficient for the use case. `EnumDisplayMonitors` (Win32) is available via `MonitorService` if we need handle-based operations later (e.g., DDC/CI).

**Pattern**:
```csharp
foreach (Screen screen in Screen.AllScreens)
{
    var form = new BlackoutForm(screen.Bounds);
    form.Show();
}
```

**Alternative considered**: `EnumDisplayMonitors` P/Invoke — more control, required for DDC/CI IOCTL, but adds complexity not needed for MVP.

---

## 3. Monitor Power-Off via Win32

**Decision**: `SendMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, (IntPtr)2)` with 500 ms delay.

**Rationale**: This is the same signal Windows uses internally. `HWND_BROADCAST = (IntPtr)0xFFFF`. The value `2` means "power off" (as opposed to `1` = low power / standby). A delay is needed so the black windows have time to be fully displayed and repainted before the monitor signal goes out.

**Constants**:
```csharp
const int WM_SYSCOMMAND   = 0x0112;
const int SC_MONITORPOWER = 0xF170;
const int POWER_OFF       = 2;
static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);
```

**P/Invoke declaration** (in `MonitorService`):
```csharp
[DllImport("user32.dll", SetLastError = false)]
static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
```

**Failure mode**: `SendMessage` returns `IntPtr.Zero` on failure but does not throw. The black windows remain as burn-in protection. No retry logic needed.

---

## 4. Workstation Lock

**Decision**: `LockWorkStation()` from `user32.dll`, called once after all `BlackoutForm` windows are closed.

**Rationale**: Standard Windows API. Called on the exit path — after input detected, before process exit. Must not be called during preview or settings mode.

**P/Invoke**:
```csharp
[DllImport("user32.dll", SetLastError = true)]
static extern bool LockWorkStation();
```

**Failure mode**: If called in a non-interactive session, returns `false`. Exception is caught and swallowed — screensaver still exits cleanly.

---

## 5. BlackoutForm — Fullscreen Borderless Window

**Decision**: WinForms `Form` with `FormBorderStyle.None`, `TopMost = true`, `ShowInTaskbar = false`, `BackColor = Color.Black`, bounds set to target screen's `Bounds`.

**Rationale**: WinForms is the constitution-mandated framework. Borderless forms with `TopMost` reliably cover system UI on all Windows versions.

**Key properties**:
```csharp
FormBorderStyle = FormBorderStyle.None;
WindowState     = FormWindowState.Normal;   // NOT Maximized — use explicit bounds
TopMost         = true;
ShowInTaskbar   = false;
BackColor       = Color.Black;
Cursor          = Cursors.None;
Bounds          = targetScreen.Bounds;      // position + size
```

**Why `WindowState.Normal` + explicit `Bounds` instead of `Maximized`**: `FormWindowState.Maximized` respects the taskbar inset on the primary monitor, leaving a gap. Explicit `Bounds` from `screen.Bounds` covers the full pixel rectangle including where the taskbar would be.

**Input interception**: Override `OnMouseMove`, `OnMouseDown`, `OnKeyDown` — fire `ExitRequested` event (subject to mouse dead-zone check).

---

## 6. Mouse Dead Zone

**Decision**: Fixed 5 px Euclidean distance from first `MouseMove` position; not configurable in MVP.

**Rationale**: Prevents accidental exit from minor cursor jitter when the screensaver activates while the user's hand is still on the mouse. 5 px is the value used by Windows's built-in screensavers.

**Implementation**: Store `_initialMousePos` on first `MouseMove`; subsequent events check `Math.Sqrt(dx²+dy²) > 5`.

---

## 7. ScreensaverHost — Lifecycle Coordination

**Decision**: A plain class (not a `Form`) that owns the `Application.Run` message loop via `ApplicationContext`.

**Rationale**: A custom `ApplicationContext` subclass is cleaner than relying on one form being the "main" form. It creates all `BlackoutForm` instances, subscribes to their `ExitRequested` event, and on first exit:
1. Closes all forms.
2. Calls `WorkstationLockService.Lock()`.
3. Exits the message loop.

**Thread safety**: All event handlers run on the UI thread (WinForms guarantee). An `int _exiting` flag (Interlocked) prevents double-exit.

---

## 8. AppSettings Persistence

**Decision**: `System.Text.Json` serialised to `%AppData%\PowerOffScreensaver\settings.json`.

**Rationale**: No registry writes (constitution Principle IV). `System.Text.Json` is built into .NET 8 — no extra dependency. File is created on first save with defaults.

**Defaults**:
```json
{
  "lockOnExit": true,
  "ddcCiEnabled": false,
  "powerOffDelayMs": 500
}
```

---

## 9. DDC/CI — Deferred Interface

**Decision**: Define `IDdcCiService` interface; ship `NullDdcCiService` (no-op) by default. Real implementation deferred post-MVP.

**Rationale**: DDC/CI requires monitor-firmware cooperation and either a Win32 IOCTL chain or a third-party NuGet (`WindowsDisplayAPI`). Neither is needed for burn-in protection or LockWorkStation. Shipping a stub avoids a hard dependency while keeping the door open.

**Interface**:
```csharp
public interface IDdcCiService
{
    bool IsSupported { get; }
    void TryPowerOff(); // swallows all exceptions
}
```

---

## 10. Build & Distribution

**Decision**: `dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true`.

**Rationale**: Produces a single `.exe` ≤ 10 MB that runs on a clean Windows machine without .NET runtime install. Renaming to `.scr` makes it a valid screensaver.

**Project file target**: `<TargetFramework>net8.0-windows</TargetFramework>` with `<UseWindowsForms>true</UseWindowsForms>`.

**No admin required**: WinForms application manifest defaults to `asInvoker` — no UAC prompt.

---

## 11. Testing Strategy

**Decision**: xUnit for service unit tests; manual multi-monitor testing for integration.

**Rationale**: Win32 P/Invoke and WinForms UI are hard to mock fully. Core logic (arg parsing, settings serialisation, dead-zone calculation) is unit-testable. End-to-end must be verified manually on a 2-3 monitor setup.

**Unit test scope**:
- `ScreensaverArgs` parser (all argument variants)
- `AppSettings` serialise / deserialise / defaults
- Dead-zone distance calculation
- `ScreensaverHost` exit-once logic (mock forms)
