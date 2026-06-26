# Feature 002 — Guaranteed Lock-on-Exit + Simplified 9-Language UI

Status: implemented
Branch: main-clean
Supersedes nothing; extends 001-power-off-screensaver.

## 1. Problem statement

Two user-reported issues:

1. **Lock is not guaranteed.** When the screensaver is active and the monitor
   has been powered off (DPMS via `SC_MONITORPOWER`), moving the mouse or
   pressing a key does **not always** lock the workstation. The user expects:
   *any* input, with 100% reliability, must (a) dismiss the screensaver and
   (b) lock the session.

2. **Settings should be simpler and localized to 9 UI languages**:
   RU, EN, DE, FR, ES, IT, PT, PL, **ZH** (matching the Steamoff project).
   The previous build shipped Ukrainian (UK) as the 9th language; it is
   replaced by Chinese (ZH) to match the requested set.

## 2. Root-cause analysis of the lock failure

The original flow relied on per-window WinForms events and a fire-and-forget
lock:

```
input -> BlackoutForm.OnMouseMove/OnKeyDown -> close forms -> LockWorkStation() -> ExitThread()
```

Failure modes identified:

| # | Cause | Effect |
|---|-------|--------|
| A | After `SC_MONITORPOWER` (monitor off), the **first** mouse move / keypress is consumed by the OS to wake the display and is **not delivered** to the foreground window. | `OnMouseMove`/`OnKeyDown` never fires for that input → nothing happens until the user moves again. |
| B | WinForms key events require the form to hold **keyboard focus**; mouse-move events only reach the form **under the cursor** and only if it is the topmost window at that pixel. Lost focus / a topmost intruder window / multi-monitor focus gaps swallow the input. | Input arrives but never triggers exit. |
| C | `LockWorkStation()` returns a `BOOL` and can **fail** (return 0) transiently or be **asynchronous**; the result was ignored. | Lock silently fails, no retry. |
| D | Forms were **closed before** locking, and the process called `ExitThread()` immediately after requesting the lock. | Brief desktop exposure; race between exit and the async lock request. |
| E | If the display stayed off (some monitor/BIOS combos), the lock screen rendered to a black panel → looked like "it didn't lock". | Perceived failure even when locked. |

## 3. Solution — layered, sequential algorithm

Detection and locking are decoupled from per-window events and made
self-verifying. Five layers run in sequence; each compensates for a failure
mode above.

### Layer 1 — Global low-level input detection (fixes A, B)
Install system-wide `WH_MOUSE_LL` and `WH_KEYBOARD_LL` hooks on the UI thread
(which has a message pump under `Application.Run`). These receive **every**
mouse move/button/wheel and **every** key event regardless of focus or which
window is under the cursor — including the display-wake input in the common
case. A 5px Euclidean dead-zone is applied to mouse moves (unchanged policy)
so jitter does not trigger. The existing per-form events are kept as a
redundant second source; both feed one `Interlocked`-guarded exit path.

### Layer 2 — Wake the display before locking (fixes E)
Before locking, send `SC_MONITORPOWER -1` (monitor ON) and assert
`SetThreadExecutionState(ES_DISPLAY_REQUIRED)` so the secure logon desktop is
actually shown rather than locking behind a dark panel.

### Layer 3 — Lock first, tear down after (fixes D)
Call the lock **while the black forms still cover the screen**. The lock
switches the session to the Winlogon secure desktop (a different desktop), so
our Default-desktop forms become irrelevant and are closed afterward with no
exposure window.

### Layer 4 — Verify and retry until confirmed (fixes C)
`LockWorkStation()` is treated as a request, not a guarantee. After each
attempt we **probe the actual lock state**: a non-elevated `OpenInputDesktop()`
succeeds for the "Default" input desktop but returns NULL (ACCESS_DENIED) once
the secure desktop is the input desktop — i.e. once locked. The guard loops:

```
for attempt in 1..MaxAttempts:
    lockService.TryLock()
    for w in 1..PollsPerAttempt:
        if probe.IsLocked(): return Confirmed
        sleep(PollDelayMs)
return probe.IsLocked()   # final check
```

### Layer 5 — Independent fallback (fixes C)
If the primary `LockWorkStation()` path keeps failing, the guard invokes a
fallback that launches `rundll32.exe user32.dll,LockWorkStation` — an
independent code path/process — before giving up.

## 4. Testability

The reliability-critical logic is extracted into pure, injectable units so it
can be unit-tested **without locking the build machine**:

- `InputGate` — baseline + dead-zone decision for raw input events (pure).
- `LockGuard` — attempt/verify/retry/fallback state machine, driven by
  injected `tryLock`, `isLocked`, `sleep`, and `fallback` delegates. Tested
  with fakes: confirms after k failed probes, exhausts attempts then succeeds,
  invokes fallback when primary never confirms, never sleeps after confirmation.
- `ILockStateProbe` / `IWorkstationLockService` — real P/Invoke behind
  interfaces; fakes used in tests.

The native hook, `OpenInputDesktop`, and `LockWorkStation` P/Invokes are thin
wrappers exercised only at runtime.

## 5. UI / localization requirements

- Languages: `en ru de fr es it pt pl zh` (UK removed, ZH added).
- Settings dialog: clearer grouping, tooltips/help text on every control,
  window auto-sizes so CJK glyphs are not clipped, keyboard-accessible.
- Language switch is instant (already supported) and persisted to
  `settings.json` (`language`).

## 6. Acceptance criteria

- AC1: Any mouse movement beyond 5px, any mouse button, any key, or wheel
  dismisses the screensaver from any focus state.
- AC2: When `LockOnExit` is enabled, exit confirms the session is locked
  (probe returns locked) or exhausts retries + fallback before exiting.
- AC3: The lock is requested while the screen is still covered; no desktop
  flash before the lock screen.
- AC4: All 9 languages render in the settings dialog without clipping.
- AC5: `dotnet test` passes, including new tests for `InputGate` and
  `LockGuard`.
