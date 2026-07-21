# Feature 004 — Reliable multi-monitor power-off (DDC/CI + auto-detect + test mode)

Status: implemented
Branch: main-clean
Extends 001/002/003.

## 1. Problem

On a 3-monitor Windows 10 setup the screensaver powers off only the middle
(primary) monitor; the side monitors stay lit. Cause: the app relied solely on
`WM_SYSCOMMAND / SC_MONITORPOWER` (DPMS), a **global** request that NVIDIA/AMD
drivers frequently apply only to the primary display. DPMS cannot reliably
address each physical monitor.

## 2. Root cause

- `SC_MONITORPOWER` is one system-wide DPMS hint, not a per-monitor command.
- GPU driver stacks (esp. NVIDIA with GeForce Experience) drop or partially
  apply it on multi-monitor rigs → some panels stay on.
- Waking: if a panel is truly off, DPMS wake from mouse input restores the
  primary but not panels that were forced off by other means.

## 3. Solution — address each monitor's hardware directly

### DDC/CI (VESA MCCS), VCP code 0xD6 "Power Mode"
Via `dxva2.dll` physical-monitor APIs we talk to each monitor over the DDC/CI
channel and set power per panel:
- 0x01 = On, 0x04 = Off (DPMS/backlight off).
Enumerate every physical monitor (`EnumDisplayMonitors` →
`GetPhysicalMonitorsFromHMONITOR`), then `SetVCPFeature(h, 0xD6, value)` on each.
`GetVCPFeatureAndVCPFeatureReply(h, 0xD6, …)` reads the current state, which
gives real **per-monitor verification** ("turned off / stayed on").

### Layered power-off (mode-driven, auto-detecting)
`PowerOffMode`: **Auto** (default) · DdcCi · Dpms · Both.
- Auto: try DDC/CI on every monitor; also fire the global DPMS broadcast to
  cover any monitor without DDC/CI support.
- DdcCi: per-monitor DDC/CI only.
- Dpms: legacy global DPMS only.
- Both: DDC/CI per monitor + DPMS broadcast.

Pure decision logic lives in `PowerPlan` (`UsesDdc`, `UseGlobalDpms`) and is
unit-tested.

### Guaranteed wake (critical)
A monitor forced off via DDC/CI does **not** come back on mouse input by itself.
The wake path therefore always sends DDC/CI **On** to every monitor plus a DPMS
On, and asserts `ES_DISPLAY_REQUIRED`. To survive any exit path, monitors are
also restored from `AppDomain.ProcessExit` / session-ending handlers.

### Test mode in Settings
A "Test monitors" dialog enumerates panels, powers them off with the selected
mode, waits, re-reads each panel's DDC/CI power state, powers back on, and shows
a per-monitor result (Off ✓ / On ✗ / Unknown). The user can switch the
power-off method and re-test; the choice is saved. This is the manual fallback
when auto-detection can't confirm a panel.

## 4. Integration / delivery

- `MonitorPowerController` orchestrates DPMS + DDC/CI by mode; used by
  `ScreensaverHost` for power-off (after delay) and wake (on exit).
- Headless `/install` flag runs `InstallerService.Install()` and writes
  `%LocalAppData%\Blackout ScreenSaver\install.log` so the build can be
  integrated into the system from the command line.
- Version bumped to 1.5.

## 5. Acceptance criteria

- AC1: On a 3-monitor rig, Auto mode powers off all panels that support DDC/CI,
  and the global DPMS covers the rest.
- AC2: Any input wakes every panel (DDC/CI On + DPMS On) and locks the session.
- AC3: The test dialog reports per-monitor off/on truthfully via DDC/CI readback.
- AC4: If the process exits unexpectedly while panels are off, ProcessExit
  restores them.
- AC5: `dotnet test` passes, including `PowerPlan` and `/install` arg tests.
