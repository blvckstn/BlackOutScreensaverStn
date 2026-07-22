# Feature 008 — Per-monitor test wizard with on-screen numbering + per-monitor mode

Status: implemented
Branch: main-clean

## 1. Problem

The user needed to tune the power-off method **per monitor** (one monitor may
sleep/wake correctly on DDC/CI, another on DPMS), but:
- All monitors report the same raw name ("Generic PnP Monitor"), so it was
  impossible to tell which is which.
- The countdown text appeared in the settings dialog, not on the monitor being
  tested.
- There was no explicit sleep/wake confirmation nor a per-monitor retry loop.

## 2. Solution

### On-screen numbering
`DdcCiService.Inventory()` returns each physical monitor's index, screen bounds
and DDC/CI support. While the test is open, a `MonitorNumberOverlay` badge (1, 2,
3…) is shown centred on every monitor (non-activating, click-through) so the user
can see which physical monitor is which.

### Per-monitor wizard (`MonitorTestForm`)
For each monitor, in sequence:
1. A large, movable `MonitorTestPrompt` window appears **on that monitor** with a
   5-second "this monitor will go dark" countdown.
2. The monitor is powered off — DDC/CI targets that single panel
   (`PowerOffOne`); DPMS is global.
3. A 15-second countdown runs while it is dark (shown on the main dialog).
4. Everything is woken (`Wake`).
5. `MonitorTestResultDialog` asks two explicit questions — did it sleep correctly,
   did it wake correctly — offers a method picker, and lets the user **Retry** this
   monitor with a different method or move to the **Next** monitor.

### Per-monitor persistence
The wizard produces an index→mode map saved to `AppSettings.PerMonitorModes`.
`MonitorPowerController.PowerOff(AppSettings)` / `Wake(AppSettings)` apply it:
DDC/CI monitors are powered off individually, and a single global DPMS is sent if
any monitor uses DPMS/Auto/Both. An empty map falls back to the global
`PowerOffMode` (unchanged behaviour). `AppSettings.Equals` compares the map by
content so record value-equality still holds.

## 3. Notes / limitations

- DPMS is inherently global (no per-monitor DPMS), so a monitor set to DPMS dims
  all of them; DDC/CI is the only truly per-monitor power-off.
- The per-monitor map is keyed by enumeration index; if the monitor order changes
  the mapping shifts (acceptable for a settings aid).

## 4. Acceptance criteria

- AC1: Each monitor shows a number badge while the test is open.
- AC2: The "will go dark" window appears on the monitor under test, movable.
- AC3: 5s warn → off → 15s dark → wake → two questions, with per-monitor retry.
- AC4: Per-monitor methods are saved and applied.
- AC5: `dotnet test` passes (162).
