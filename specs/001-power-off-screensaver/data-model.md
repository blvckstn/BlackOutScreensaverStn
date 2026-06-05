# Data Model: PowerOffScreensaver

**Phase 1 output** — entities, state transitions, and persistence schema.

---

## Entities

### 1. `ScreensaverArgs`

Parsed representation of the command-line arguments passed by Windows.

| Field | Type | Description |
|---|---|---|
| `Mode` | `enum LaunchMode` | Screensaver / Settings / Preview |
| `PreviewHwnd` | `IntPtr?` | Only set when `Mode = Preview` |

**Enum `LaunchMode`**:

| Value | Trigger |
|---|---|
| `Screensaver` | `/s` |
| `Settings` | `/c` or `/c:HWND` or no args |
| `Preview` | `/p HWND` |

**Parsing rules**:
- Case-insensitive argument matching.
- `/c:12345` → `Mode = Settings`, HWND ignored.
- `/p 12345` → `Mode = Preview`, `PreviewHwnd = 12345`.
- Unknown args → `Mode = Settings` (safe default).

**Validation**: None required — args are OS-provided.

---

### 2. `AppSettings`

User-configurable preferences. Persisted to disk. Loaded once at startup; saved on user action in `SettingsForm`.

| Field | Type | Default | Description |
|---|---|---|---|
| `LockOnExit` | `bool` | `true` | Call `LockWorkStation()` when screensaver exits |
| `DdcCiEnabled` | `bool` | `false` | Attempt DDC/CI VCP power-off (opt-in) |
| `PowerOffDelayMs` | `int` | `500` | Milliseconds to wait after windows shown before sending power-off signal |

**Constraints**:
- `PowerOffDelayMs` ∈ [0, 5000].
- Stored at `%AppData%\PowerOffScreensaver\settings.json`.
- File is created with defaults on first launch if absent.
- Corrupt / missing file → defaults applied silently; no crash.

**JSON schema** (`settings.json`):
```json
{
  "lockOnExit": true,
  "ddcCiEnabled": false,
  "powerOffDelayMs": 500
}
```

---

### 3. `MonitorInfo`

Runtime snapshot of a connected display, derived from `Screen.AllScreens` at launch time.

| Field | Type | Source | Description |
|---|---|---|---|
| `Bounds` | `Rectangle` | `Screen.Bounds` | Pixel rectangle (position + size) in virtual screen coordinates |
| `DeviceName` | `string` | `Screen.DeviceName` | Win32 device name, e.g., `\\.\DISPLAY1` |
| `IsPrimary` | `bool` | `Screen.Primary` | Whether this is the primary display |

**Lifecycle**:
- Created once at `/s` launch from `Screen.AllScreens`.
- Immutable during screensaver run — no dynamic monitor change detection in MVP.
- Not persisted.

---

### 4. `BlackoutFormState` (implicit in `BlackoutForm`)

Internal state managed by each `BlackoutForm` instance.

| State | Meaning |
|---|---|
| `Idle` | Window shown, waiting for input |
| `ExitPending` | First input received, `ExitRequested` event fired |

**Transition**:
```
Idle ──[mouse move > 5px / click / key press]──► ExitPending ──► ExitRequested event
```

- Once `ExitPending`, no further events are processed (flag guard).
- `ExitRequested` is raised exactly once per `BlackoutForm` instance.

---

## State Transitions — Screensaver Lifecycle

```
[Launch /s]
     │
     ▼
[Parse args → LaunchMode.Screensaver]
     │
     ▼
[Load AppSettings]
     │
     ▼
[ScreensaverHost: enumerate Screen.AllScreens → create BlackoutForm per screen]
     │
     ▼
[All BlackoutForms shown & painted]
     │
     ▼ (after PowerOffDelayMs)
[MonitorPowerService: SendMessage(WM_SYSCOMMAND, SC_MONITORPOWER, 2)]
     │
     ▼
[Waiting for input on any BlackoutForm]
     │
     ├──[First ExitRequested from any form]
     │        │
     │        ▼
     │   [ScreensaverHost: close all forms]
     │        │
     │        ▼ (if LockOnExit)
     │   [WorkstationLockService: LockWorkStation()]
     │        │
     │        ▼
     │   [Application.Exit()]
     │
     └──[no input] (loop continues)
```

---

## Persistence

| What | Where | Format | Created by |
|---|---|---|---|
| `AppSettings` | `%AppData%\PowerOffScreensaver\settings.json` | UTF-8 JSON | `SettingsService` on first save |

No database, no registry, no network.

---

## Relationships

```
ScreensaverArgs
    └── mode determines which path ScreensaverHost follows

ScreensaverHost
    ├── reads AppSettings (1..1)
    ├── creates BlackoutForm (1..N, one per MonitorInfo)
    ├── owns MonitorPowerService (1..1)
    ├── owns WorkstationLockService (1..1)
    └── owns IDdcCiService (1..1, default NullDdcCiService)

BlackoutForm
    └── fires ExitRequested → consumed by ScreensaverHost
```
