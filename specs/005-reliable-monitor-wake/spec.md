# Feature 005 — Reliable monitor wake after DDC/CI power-off

Status: implemented
Branch: main-clean
Fixes a regression introduced by 004.

## 1. Problem

After 004 turned monitors off over DDC/CI (VCP 0xD6 = Off), the monitors would
not reliably wake. The user had to physically power-cycle the monitors and press
Ctrl+Alt+Del to get a picture back.

## 2. Root cause

- DDC/CI "Off" (0x04) is a different state than DPMS off. Sending DDC/CI "On"
  (0x01) does not reliably reach a panel whose controller has powered down, and
  DPMS wake (mouse move) does not exit the DDC/CI-off state.
- The old wake fired DDC/CI On once and immediately locked + exited, without
  waiting for or confirming that the panels actually came back, and without a
  real input event or a signal re-assertion. Locking before the display woke is
  what forced the Ctrl+Alt+Del (the secure-desktop switch re-asserts the signal).

## 3. Solution — verified, multi-technique wake before lock

`MonitorPowerController.WakeVerified()` runs, in order, and repeats until
confirmed (`DisplaySignal` provides the primitives):

1. `SetThreadExecutionState(ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED)` — mark the
   display required so power management keeps it on through the lock.
2. `SendInput` a benign key (F15) — a real input event is the OS-sanctioned way
   to leave DPMS sleep.
3. `SC_MONITORPOWER` On — re-assert the GPU video signal.
4. DDC/CI On (0x01) to every monitor.
5. Read back each monitor's DDC/CI power state and check `WakePlan.AllAwake`
   (every verifiable panel reports On). If not, retry from step 2.
6. On the final attempt, escalate to `ChangeDisplaySettingsEx` (re-apply the
   display mode) to force a panel stuck at "no signal" to re-sync — the software
   equivalent of the Ctrl+Alt+Del re-assert.

The whole sequence runs **before** `LockWorkStation`, so the session never
switches to the secure desktop while a panel is asleep. The outcome (per-monitor
before → after, attempts, elapsed, whether all confirmed awake) is appended to
`%LocalAppData%\Blackout ScreenSaver\wake.log`.

`WakePlan.AllAwake` / `AnyVerifiable` are pure and unit-tested. The exit and
`ProcessExit` safety-net paths use a single best-effort `PowerOn()` that never
blocks.

## 4. Acceptance criteria

- AC1: After a DDC/CI power-off, input restores every monitor without a physical
  power cycle or Ctrl+Alt+Del.
- AC2: Wake completes and is confirmed (or exhausts retries + re-detect) before
  the session locks.
- AC3: `wake.log` records the per-monitor before→after state each wake.
- AC4: The settings monitor test reports how many monitors confirmed wake.
- AC5: `dotnet test` passes, including `WakePlan` tests.
