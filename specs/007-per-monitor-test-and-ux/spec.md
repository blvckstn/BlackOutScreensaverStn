# Feature 007 — Per-monitor test wizard + settings UX fix

Status: implemented
Branch: main-clean

## 1. Problem

1. The settings dialog layout was broken: the power-off method combo overlapped
   its label and clipped its text, and two different buttons were both labelled
   "Тест" (the screensaver-launch button and the monitor test).
2. The monitor test powered all monitors off at once, so the user could not tell
   which method each individual monitor responds to (one monitor may prefer
   DDC/CI, another DPMS).

## 2. Solution

### Per-monitor sequential test (`MonitorTestForm`)
The test now steps through each monitor one at a time:
- Shows "Monitor N of M — <description>" and a **5-second countdown** with
  "This monitor will go dark now".
- Powers **that monitor** off: DDC/CI modes target the single panel
  (`DdcCiService.PowerOne` / `MonitorPowerController.PowerOffOne`); DPMS is global
  (no per-monitor DPMS exists) so it dims all.
- Keeps it dark briefly, reads the panel state, then wakes everything back.
- Asks the user Yes/No about **that specific monitor**, and records the result per
  row. Black-screen-only has nothing to power off, so it is skipped with a note.

Per-monitor DDC targeting is added via `ForEachPhysical(action, onlyIndex)` and
`PowerOne(index, on)` / `ProbeOne(index)` keyed by the stable enumeration index.

### Settings UX (per ui-ux-pro-max principles)
- Method label and a wide combo on one row with **no overlap**; the combo is wide
  enough for every localized option.
- Removed the duplicate "Тест" (screensaver-launch) button.
- Secondary actions ("Тест мониторов…", "Проверить") on the left, primary
  OK/Cancel bottom-right; consistent button sizing and spacing.

## 3. Acceptance criteria

- AC1: The method combo no longer overlaps its label and shows the full option text.
- AC2: No two buttons share the same label.
- AC3: The test runs monitor-by-monitor with a 5s "this monitor will go dark"
  countdown and a per-monitor Yes/No confirmation.
- AC4: DDC/CI modes power off only the target monitor during its step.
- AC5: `dotnet test` passes.
