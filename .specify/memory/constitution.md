<!--
SYNC IMPACT REPORT
==================
Version change: 1.0.0 → 1.1.0
Added sections: Principle VI (Russian Language for Spec Kit Outputs)
Removed sections: none
Modified sections: Governance (amended procedure note)
Templates requiring updates:
  ✅ plan-template.md — Constitution Check gates updated to include Principle VI
  ✅ spec-template.md — aligned; no mandatory updates needed
  ✅ tasks-template.md — aligned; no specific updates needed
Deferred TODOs: none
-->

# PowerOffScreensaver Constitution

## Core Principles

### I. Multi-Monitor Coverage (NON-NEGOTIABLE)

The application MUST detect all connected displays at runtime using
`Screen.AllScreens` (WinForms) or equivalent Win32 enumeration via
`EnumDisplayMonitors`. A dedicated fullscreen black window MUST be created
on every detected screen — not just the primary. Failure to cover any
connected monitor is a critical bug, not a degraded-mode scenario.

**Rationale**: The entire reason this tool exists is that Windows's built-in
"turn off display" command fails on secondary monitors. Partial coverage
defeats the purpose.

### II. Standard Screensaver Protocol (NON-NEGOTIABLE)

The executable MUST handle all three standard Windows screensaver arguments:
- `/s` — activate screensaver (full run mode)
- `/c` or `/c:HWND` — show settings dialog
- `/p HWND` — preview mode (minimal stub acceptable; must not crash)

The binary MUST be compilable to a self-contained `.exe` that Windows accepts
when renamed to `.scr`. No installer, no registry writes at install time, no
elevation prompts on launch.

**Rationale**: Standard protocol compliance ensures the OS can manage the
screensaver lifecycle normally (timeout, password lock, preview).

### III. Win32 Isolation

Every call to the Win32 API, DDC/CI, or any P/Invoke MUST live in a dedicated
service class under `src/Services/`. No P/Invoke declarations or
`DllImport` attributes are permitted in Form, Program, or UI classes.

Affected service classes (minimum):
- `MonitorService` — display enumeration and Win32 power commands
- `InputService` — global mouse/keyboard hook for exit detection
- `ScreensaverService` — screensaver lifecycle coordination
- `DdcCiService` — optional DDC/CI VCP power commands

**Rationale**: Keeps platform-specific complexity auditable and testable in
isolation. Any reviewer can inspect one file to understand all OS interactions.

### IV. Minimal Footprint

The application MUST NOT:
- Use Electron, Flutter, CEF, or any browser-based rendering stack
- Require .NET runtime installation when published as self-contained
- Write to the registry during normal operation
- Modify Windows power plan settings without an explicit user action in `/c` settings
- Require elevated (Administrator) privileges for the `/s` screensaver path

**Preferred stack**: C# / .NET 8 / WinForms / Win32 API (P/Invoke).
Self-contained single-file publish (`dotnet publish -r win-x64 --self-contained`).

**Rationale**: A screensaver is a low-level OS component. Heavy runtimes
create startup latency, memory overhead, and deployment friction.

### V. Safety & Security (NON-NEGOTIABLE)

The application MUST NOT:
- Capture, log, or transmit keystrokes (keyboard hook is read-only exit trigger)
- Capture, log, or transmit mouse positions beyond movement detection
- Run as a hidden background service or system tray process
- Request network access
- Store credentials or personal data

The exit trigger MUST call `LockWorkStation()` after terminating the
screensaver windows — this is the expected Windows screensaver behaviour.

**Rationale**: Screensavers run under user trust. Any deviation from pure
display-protection function violates that trust and can be flagged by
antivirus as keylogger behaviour.

### VI. Russian Language for Spec Kit Outputs (NON-NEGOTIABLE for Governance)

All Spec Kit artifacts (specifications, plans, tasks, analyses, reports,
commit comments) MUST be written in Russian for clarity and accessibility
to the local team.

**Exceptions** (remain in English):
- File names, folder names, class names, method names, APIs remain English
  (as per project conventions)
- Terminal commands and code identifiers (variables, constants, functions)
- Commit messages may be English only if explicitly required

**Style requirements**:
- Russian language; concise and to-the-point
- No unnecessary verbosity
- Results first, details after
- For development tasks: always provide specific file paths, commands, and verification steps

**Rationale**: Unified language in documentation ensures clarity for Russian-speaking
team members, lowers comprehension barriers, and guarantees all processes are
documented accessibly. Code and identifiers remain English to maintain
consistency with international development conventions.

## Technology Stack

- **Language**: C# 12 / .NET 8
- **UI framework**: WinForms (for multi-monitor window management)
- **Platform APIs**: Win32 P/Invoke — `SendMessage(WM_SYSCOMMAND, SC_MONITORPOWER)`,
  `EnumDisplayMonitors`, `SetThreadExecutionState`, `LockWorkStation`
- **Optional**: DDC/CI via `WindowsDisplayAPI` NuGet or direct IOCTL calls
- **Build**: `dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true`
- **Testing**: manual on 2-3 monitor setup; xUnit for unit-testable service logic

## Power Management Strategy

Two-tier approach, applied in order:

1. **Primary**: Send `WM_SYSCOMMAND SC_MONITORPOWER 2` to the desktop HWND.
   This is the same signal Windows uses natively and works on most setups.

2. **Fallback A**: If primary command does not visibly power off all monitors
   within 2 seconds, keep the black fullscreen windows active. Black windows
   prevent burn-in even without hardware power-off.

3. **Fallback B (optional)**: If the monitor advertises DDC/CI support,
   send VCP Feature 0xD6 (Display Power Mode) value 0x04 (Off) via
   `DdcCiService`. This is opt-in and gracefully skipped on monitors that
   do not support DDC/CI.

No approach MUST be assumed infallible. The combination guarantees burn-in
protection even when hardware power-off is unavailable.

## Governance

This constitution supersedes all other coding guidelines for this repository.
Any PR that violates Principles I, II, V, or VI MUST NOT be merged regardless
of functionality. Principles III and IV violations require documented
justification in the PR description.

Amendment procedure:
1. Update this file with a rationale for each changed principle.
2. Increment `CONSTITUTION_VERSION` per semantic versioning rules.
3. Update `LAST_AMENDED_DATE` to the amendment date.
4. Re-run `/speckit-constitution` to propagate changes to templates.

The `CLAUDE.md` file is the runtime agent guidance file. Keep it aligned with
this constitution when architectural decisions change.

**Version**: 1.1.0 | **Ratified**: 2026-06-05 | **Last Amended**: 2026-06-05
