# Quickstart & Validation Guide: PowerOffScreensaver

**Phase 1 output** — runnable scenarios that prove the feature works end-to-end.

---

## Prerequisites

| Requirement | Check command |
|---|---|
| .NET 8 SDK | `dotnet --version` (must show `8.x.x`) |
| Windows 10/11 x64 | `winver` |
| 2+ monitors (for full multi-monitor test) | Display Settings → Multiple displays |

---

## Build

```powershell
# From repo root
dotnet build src/PowerOffScreensaver/PowerOffScreensaver.csproj -c Release
```

**Expected**: `Build succeeded. 0 Error(s)  0 Warning(s)`

### Self-contained publish

```powershell
dotnet publish src/PowerOffScreensaver/PowerOffScreensaver.csproj `
    -c Release -r win-x64 --self-contained `
    -p:PublishSingleFile=true `
    -o publish/
```

**Expected**: `publish/PowerOffScreensaver.exe` ≤ 10 MB.

---

## Scenario 1 — Screensaver mode (`/s`)

```powershell
.\publish\PowerOffScreensaver.exe /s
```

**Expected**:
1. Black fullscreen windows appear on **all** connected monitors within 500 ms.
2. Cursor is hidden.
3. After ~500 ms delay, monitor power-off signal is sent (monitors may go dark).
4. Moving the mouse > 5 px OR pressing any key exits all windows immediately.
5. Windows Lock Screen appears after exit (workstation locked).

**Multi-monitor check**: Temporarily disconnect one monitor, re-run — only connected monitors should be covered.

---

## Scenario 2 — Settings dialog (`/c`)

```powershell
.\publish\PowerOffScreensaver.exe /c
```

**Expected**:
1. A small settings window opens — NOT the screensaver.
2. Checkboxes present: "Lock workstation on exit", "Try DDC/CI power off".
3. Slider/spinner for power-off delay (ms).
4. "Test" button launches screensaver mode (same as `/s`).
5. Closing the window saves changes to `%AppData%\PowerOffScreensaver\settings.json`.

**Verify persistence**:
```powershell
# After changing a setting and closing the dialog:
Get-Content "$env:APPDATA\PowerOffScreensaver\settings.json"
```

---

## Scenario 3 — Preview mode (`/p`)

```powershell
.\publish\PowerOffScreensaver.exe /p 0
```

**Expected**: Process exits immediately with exit code 0 (no window shown).

```powershell
# Verify exit code
$LASTEXITCODE  # should be 0
```

---

## Scenario 4 — `.scr` install test

```powershell
# Copy to Windows screensaver directory (no admin needed for user profile test)
Copy-Item publish\PowerOffScreensaver.exe "$env:WINDIR\System32\PowerOffScreensaver.scr"

# Or test rename in place:
Copy-Item publish\PowerOffScreensaver.exe publish\PowerOffScreensaver.scr
.\publish\PowerOffScreensaver.scr /s
```

**Expected**: Same behaviour as Scenario 1.

**System install**: Right-click `PowerOffScreensaver.scr` → "Install". Open Screen Saver Settings — "PowerOffScreensaver" should appear in the dropdown.

---

## Scenario 5 — No-crash on single monitor

Run Scenario 1 on a machine with one monitor. Expected: one black window, same input/exit behaviour.

---

## Scenario 6 — Settings defaults

Delete `%AppData%\PowerOffScreensaver\settings.json` (or run on a fresh machine), then run `/s`.

**Expected**: App uses defaults — lock enabled, DDC/CI disabled, 500 ms delay — without crashing.

---

## Unit Tests

```powershell
dotnet test src/PowerOffScreensaver.Tests/PowerOffScreensaver.Tests.csproj
```

**Expected**: All tests pass. Key test classes:
- `ScreensaverArgsParserTests` — all argument variants
- `AppSettingsTests` — serialise/deserialise/defaults
- `DeadZoneTests` — 5 px boundary conditions
- `ScreensaverHostExitOnceTests` — idempotent exit guard

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| Monitor doesn't go dark after `/s` | `WM_SYSCOMMAND` not honoured by GPU driver | Black windows still protect from burn-in; check README troubleshooting section |
| Windows Lock Screen doesn't appear | `LockWorkStation()` failed in session | Check Event Viewer → Windows Logs → System for "Winlogon" errors |
| Only primary monitor goes black | Bug in `Screen.AllScreens` enumeration | Open issue; workaround: run with one monitor |
| App exits immediately on launch | Antivirus blocking | Add exclusion; check Windows Defender quarantine |
| `dotnet build` fails | Wrong .NET version | Run `dotnet --version`; install .NET 8 SDK |
