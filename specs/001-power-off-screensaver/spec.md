# Feature Specification: PowerOffScreensaver

**Feature Branch**: `001-power-off-screensaver`

**Created**: 2026-06-05

**Status**: Draft

**Input**: Windows screensaver that powers off all connected monitors and locks the workstation on exit.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Activate screensaver via `/s` (Priority: P1)

A user double-clicks `PowerOffScreensaver.scr` from Screen Saver Settings (or Windows activates it on timeout). The screensaver covers every connected monitor with a black fullscreen window, sends the monitor power-off command, and exits cleanly on any mouse move / key press, locking the workstation.

**Why this priority**: Core product value — without this, the app does nothing useful.

**Independent Test**: Build the executable, rename to `.scr`, run `PowerOffScreensaver.exe /s` — all monitors go black, any input exits and locks workstation.

**Acceptance Scenarios**:

1. **Given** the user has 2+ monitors connected, **When** `/s` is launched, **Then** a black borderless fullscreen window appears on every display within 500 ms.
2. **Given** the screensaver is active, **When** the user moves the mouse more than a small dead zone, **Then** all windows close and `LockWorkStation()` is called.
3. **Given** the screensaver is active, **When** the user presses any key, **Then** all windows close and `LockWorkStation()` is called.
4. **Given** the `WM_SYSCOMMAND SC_MONITORPOWER` command fails silently, **Then** the black windows remain (burn-in protection) and the app does not crash.

---

### User Story 2 — Settings dialog via `/c` (Priority: P2)

A user opens Screen Saver Settings and clicks "Settings". A minimal dialog appears with options to configure lock-on-exit, DDC/CI power-off, and power-off delay.

**Why this priority**: Required by Windows screensaver protocol for the `.scr` rename to work correctly in the OS dialog.

**Independent Test**: Run `PowerOffScreensaver.exe /c` — a settings window opens, changes persist to disk, window closes cleanly.

**Acceptance Scenarios**:

1. **Given** `/c` argument is passed, **When** the app starts, **Then** `SettingsForm` opens (not the screensaver).
2. **Given** the settings dialog is open, **When** the user changes "Lock on exit" checkbox and saves, **Then** the preference is persisted and loaded on next launch.
3. **Given** `/c:HWND` variant is passed, **Then** app opens settings dialog without crashing (HWND ignored for now).

---

### User Story 3 — Preview mode via `/p` (Priority: P3)

Windows Screen Saver Settings shows a miniature preview in the preview pane. The app handles `/p HWND` without crashing.

**Why this priority**: Required by protocol; minimal implementation acceptable (no actual preview rendering needed).

**Independent Test**: Run `PowerOffScreensaver.exe /p 12345` — app starts and exits cleanly with exit code 0.

**Acceptance Scenarios**:

1. **Given** `/p HWND` is passed, **When** the app starts, **Then** it exits cleanly without displaying any window.

---

### User Story 4 — DDC/CI optional power-off (Priority: P4)

On monitors that support DDC/CI, the screensaver also sends VCP Feature 0xD6 (Display Power Mode = Off). On monitors that don't support it, the app silently skips DDC/CI without degradation.

**Why this priority**: Enhancement — app functions correctly without it.

**Independent Test**: Enable DDC/CI in settings; run `/s`; verify no crash even on monitors without DDC/CI support.

**Acceptance Scenarios**:

1. **Given** DDC/CI is enabled in settings, **When** `/s` launches, **Then** VCP 0xD6 is attempted after the black windows open.
2. **Given** DDC/CI is enabled but monitor rejects the command, **Then** app continues running and does not crash.

---

### Edge Cases

- What happens when `Screen.AllScreens` returns only one entry on a multi-monitor system? → App covers what Windows reports; user troubleshooting note in README.
- How does the system handle a monitor being disconnected between launch and power-off? → P/Invoke failures are caught and logged; app does not crash.
- What if `LockWorkStation()` fails (e.g., no interactive session)? → Exception is caught; screensaver exits cleanly.
- What if the user launches `/s` without any monitors being detected? → App exits immediately with no visible output.
- Concurrent input events on multiple BlackoutForms triggering exit simultaneously → Only the first exit signal is processed; subsequent ones are no-ops.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: App MUST create a borderless fullscreen black window on every screen reported by `Screen.AllScreens` within 500 ms of launch.
- **FR-002**: App MUST send `WM_SYSCOMMAND SC_MONITORPOWER 2` after a configurable delay (default 500 ms) following window creation.
- **FR-003**: App MUST exit all screensaver windows and call `LockWorkStation()` on first mouse-move (beyond 5 px dead zone), click, or key press.
- **FR-004**: App MUST handle `/s`, `/c` (and `/c:HWND`), and `/p HWND` command-line arguments.
- **FR-005**: The binary MUST run when renamed to `.scr` without installer or admin elevation.
- **FR-006**: All Win32 P/Invoke declarations MUST reside exclusively in `src/Services/` classes.
- **FR-007**: App MUST NOT capture, store, or transmit keystrokes or mouse coordinates.
- **FR-008**: App MUST NOT require network access or write to the registry during normal `/s` operation.
- **FR-009**: Settings MUST be persisted to a JSON file in `%AppData%\PowerOffScreensaver\settings.json`.
- **FR-010**: DDC/CI power-off MUST be opt-in and gated behind the `IDdcCiService` interface; a `NullDdcCiService` stub MUST be the default.

### Key Entities

- **AppSettings**: User preferences — lock-on-exit flag, DDC/CI-enabled flag, power-off delay (ms).
- **ScreensaverArgs**: Parsed command-line — mode (Screensaver / Settings / Preview), optional HWND.
- **MonitorInfo**: Per-display metadata — bounds (Rectangle), device name, is-primary flag.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All connected monitors are covered by black windows within 500 ms of `/s` launch.
- **SC-002**: Any mouse/keyboard input exits the screensaver within 100 ms of detection.
- **SC-003**: `LockWorkStation()` is called on every clean exit of the `/s` path.
- **SC-004**: App binary size < 10 MB self-contained single-file publish (no runtime install required).
- **SC-005**: App launches without UAC prompt on standard user account.
- **SC-006**: `dotnet build` completes with 0 errors and 0 warnings on a clean clone.

## Assumptions

- Target OS: Windows 10 / Windows 11 (x64). No ARM64 or 32-bit support required for v1.
- Single executable distribution — no installer, no MSI.
- Settings storage via JSON in `%AppData%`; no registry writes.
- DDC/CI support is monitor-firmware-dependent; graceful degradation is mandatory.
- Preview mode (`/p`) renders no content — just a no-op exit to satisfy screensaver protocol.
- `LockWorkStation()` behaviour on non-interactive sessions is out of scope; errors are swallowed.

## Clarifications

### Session 2026-06-05

- Q: Should the dead zone for mouse movement be configurable? → A: Fixed at 5 px for MVP; can be made configurable in settings later.
- Q: Should settings persist across the binary rename to `.scr`? → A: Yes — settings are keyed by `%AppData%\PowerOffScreensaver\` independent of binary name.
- Q: Should DDC/CI use a NuGet package or direct IOCTL? → A: Start with a `NullDdcCiService` stub (interface only); real DDC/CI implementation deferred to post-MVP.
