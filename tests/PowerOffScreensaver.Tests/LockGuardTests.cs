using System;
using PowerOffScreensaver;
using Xunit;

namespace PowerOffScreensaver.Tests;

public class LockGuardTests
{
    [Fact]
    public void AlreadyLocked_ReturnsConfirmed_WithoutLockingOrSleeping()
    {
        int tryLockCalls = 0, sleepCalls = 0;
        var guard = new LockGuard(
            tryLock: () => { tryLockCalls++; return true; },
            isLocked: () => true,
            sleep: _ => sleepCalls++);

        Assert.Equal(LockResult.Confirmed, guard.Ensure());
        Assert.Equal(0, tryLockCalls);
        Assert.Equal(0, sleepCalls);
    }

    [Fact]
    public void LocksOnFirstAttempt_ReturnsConfirmed_NoSleep()
    {
        int probe = 0, tryLockCalls = 0, sleepCalls = 0;
        var guard = new LockGuard(
            tryLock: () => { tryLockCalls++; return true; },
            isLocked: () => probe++ >= 1, // first probe false, then locked
            sleep: _ => sleepCalls++);

        Assert.Equal(LockResult.Confirmed, guard.Ensure());
        Assert.Equal(1, tryLockCalls);
        Assert.Equal(0, sleepCalls);
    }

    [Fact]
    public void LocksAfterSeveralPolls_SleepsBetweenProbes()
    {
        int probe = 0, sleepCalls = 0;
        var guard = new LockGuard(
            tryLock: () => true,
            isLocked: () => probe++ >= 3, // locked on the 4th probe
            sleep: _ => sleepCalls++,
            pollsPerAttempt: 5);

        Assert.Equal(LockResult.Confirmed, guard.Ensure());
        Assert.Equal(2, sleepCalls); // probes: top + poll0 + poll1 (2 sleeps) + poll2(true)
    }

    [Fact]
    public void PrimaryFails_FallbackLocks_ReturnsConfirmedByFallback()
    {
        bool locked = false;
        int fallbackCalls = 0;
        var guard = new LockGuard(
            tryLock: () => false,          // primary never makes it locked
            isLocked: () => locked,
            sleep: _ => { },
            fallback: () => { fallbackCalls++; locked = true; return true; },
            maxAttempts: 3,
            pollsPerAttempt: 2,
            pollDelayMs: 0);

        Assert.Equal(LockResult.ConfirmedByFallback, guard.Ensure());
        Assert.Equal(1, fallbackCalls);
    }

    [Fact]
    public void NothingLocks_ReturnsUnverified()
    {
        var guard = new LockGuard(
            tryLock: () => false,
            isLocked: () => false,
            sleep: _ => { },
            fallback: () => false,
            maxAttempts: 2,
            pollsPerAttempt: 1,
            pollDelayMs: 0);

        Assert.Equal(LockResult.Unverified, guard.Ensure());
    }

    [Fact]
    public void MaxAttempts_IsHonored()
    {
        int tryLockCalls = 0;
        var guard = new LockGuard(
            tryLock: () => { tryLockCalls++; return false; },
            isLocked: () => false,
            sleep: _ => { },
            fallback: null,
            maxAttempts: 4,
            pollsPerAttempt: 1,
            pollDelayMs: 0);

        guard.Ensure();
        Assert.Equal(4, tryLockCalls);
    }

    [Fact]
    public void NoFallbackProvided_StillReturnsUnverified()
    {
        var guard = new LockGuard(
            tryLock: () => false,
            isLocked: () => false,
            sleep: _ => { },
            fallback: null,
            maxAttempts: 1,
            pollsPerAttempt: 1,
            pollDelayMs: 0);

        Assert.Equal(LockResult.Unverified, guard.Ensure());
    }

    [Fact]
    public void NullDelegates_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new LockGuard(null!, () => true, _ => { }));
        Assert.Throws<ArgumentNullException>(() => new LockGuard(() => true, null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => new LockGuard(() => true, () => true, null!));
    }
}
