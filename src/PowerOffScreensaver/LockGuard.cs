using System;

namespace PowerOffScreensaver;

/// <summary>
/// The outcome of a guaranteed-lock attempt.
/// </summary>
public enum LockResult
{
    /// <summary>The session was verified locked via the primary path.</summary>
    Confirmed,
    /// <summary>The session was verified locked, but only after the fallback ran.</summary>
    ConfirmedByFallback,
    /// <summary>Every attempt (and the fallback) ran but the lock could not be verified.</summary>
    Unverified
}

/// <summary>
/// Layers 3-5 of the lock spec as a pure, injectable state machine: attempt the
/// lock, poll the real lock state, retry with backoff, then run an independent
/// fallback before giving up. All side effects are delegates so the policy can
/// be unit-tested without ever locking the build machine.
/// </summary>
public sealed class LockGuard
{
    private readonly Func<bool> _tryLock;     // primary lock request (LockWorkStation)
    private readonly Func<bool> _isLocked;    // probe: is the session actually locked?
    private readonly Action<int> _sleep;      // delay between polls
    private readonly Func<bool>? _fallback;   // independent last-resort lock (rundll32)
    private readonly int _maxAttempts;
    private readonly int _pollsPerAttempt;
    private readonly int _pollDelayMs;

    public LockGuard(
        Func<bool> tryLock,
        Func<bool> isLocked,
        Action<int> sleep,
        Func<bool>? fallback = null,
        int maxAttempts = 3,
        int pollsPerAttempt = 5,
        int pollDelayMs = 120)
    {
        _tryLock = tryLock ?? throw new ArgumentNullException(nameof(tryLock));
        _isLocked = isLocked ?? throw new ArgumentNullException(nameof(isLocked));
        _sleep = sleep ?? throw new ArgumentNullException(nameof(sleep));
        _fallback = fallback;
        _maxAttempts = Math.Max(1, maxAttempts);
        _pollsPerAttempt = Math.Max(1, pollsPerAttempt);
        _pollDelayMs = Math.Max(0, pollDelayMs);
    }

    /// <summary>
    /// Requests the lock and does not return until it is verified, the attempts
    /// (plus fallback) are exhausted. Returns how the lock was ultimately
    /// achieved (or that it could not be verified).
    /// </summary>
    public LockResult Ensure()
    {
        // Already locked (e.g. user hit Win+L during the race)? Done.
        if (_isLocked())
            return LockResult.Confirmed;

        for (int attempt = 0; attempt < _maxAttempts; attempt++)
        {
            _tryLock();
            if (PollUntilLocked())
                return LockResult.Confirmed;
        }

        if (_fallback is not null)
        {
            _fallback();
            if (PollUntilLocked())
                return LockResult.ConfirmedByFallback;
        }

        return _isLocked() ? LockResult.Confirmed : LockResult.Unverified;
    }

    private bool PollUntilLocked()
    {
        for (int poll = 0; poll < _pollsPerAttempt; poll++)
        {
            if (_isLocked())
                return true;
            _sleep(_pollDelayMs);
        }
        return _isLocked();
    }
}
