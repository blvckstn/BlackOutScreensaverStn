# Feature 006 — DisplayPort recover, black-only mode, test confirmation

Status: implemented
Branch: main-clean

## 1. Problem

On a 3-monitor rig (two DisplayPort side monitors + one HDMI middle), after the
screensaver powered the monitors off:
- ~10s later the side monitors lit up showing the **desktop**, the middle showed
  the black window; and
- Windows played **device connect/disconnect sounds**.

## 2. Root cause

DisplayPort Hot-Plug Detect (HPD). When a DP monitor enters power-off, the DP
link drops and Windows treats the monitor as **unplugged** (the chime), removes
it from the display topology, and rearranges the desktop onto the remaining
monitors. Moments later the link re-negotiates and the monitor is re-added (the
second chime). The screensaver's black windows were created for the original
layout, so the returning DP monitors are no longer covered → desktop shows
through. HDMI does not drop, so the middle window stays. (Nothing is logged to
the System event log because this is a driver-level HPD, not a PnP removal.)

## 3. Solution

1. **Re-cover on layout change.** `ScreensaverHost` subscribes to
   `SystemEvents.DisplaySettingsChanged` and rebuilds the black windows to cover
   whatever monitors currently exist (marshalled to the UI thread, coalesced).
   Power-off is **not** re-issued on change, to avoid a hot-unplug loop — only
   coverage is restored, so the desktop never shows through.
2. **`PowerOffMode.None` — "Black screen only".** Covers the screens with black
   windows and never powers the monitors off. DisplayPort links never drop, so
   there are no connect/disconnect sounds and no rearrange. Best for setups where
   power-off misbehaves. Wake is trivial (nothing was powered off).
3. **Test confirmation.** After the monitor test runs (off → verify → on), it now
   asks the user Yes/No "did it work correctly — did every monitor go dark and
   come back?". On **No** it falls back to Black-screen-only. This captures what
   DDC/CI readback can't (e.g. the DP re-connect quirk).

`PowerPlan.UsesDdc(None)` and `UseGlobalDpms(None, …)` are false; unit-tested.

## 4. Acceptance criteria

- AC1: After a DP hot-unplug/replug, the desktop never shows on any monitor —
  black coverage follows the layout.
- AC2: Black-screen-only mode produces no power-off, no DP link drop, no sounds.
- AC3: The test asks for confirmation and falls back to Black-screen-only on "No".
- AC4: `dotnet test` passes (PowerPlan None cases).
