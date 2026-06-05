using System;
using System.Threading;
using System.Threading.Tasks;
using PowerOffScreensaver;
using Xunit;

namespace PowerOffScreensaver.Tests;

public class ScreensaverHostExitOnceTests
{
    [Fact]
    public void ExitGuard_SingleInterlocked_SucceedsOnFirstAttempt()
    {
        int exiting = 0;
        bool firstAttempt = Interlocked.CompareExchange(ref exiting, 1, 0) == 0;

        Assert.True(firstAttempt);
        Assert.Equal(1, exiting);
    }

    [Fact]
    public void ExitGuard_SingleInterlocked_FailsOnSecondAttempt()
    {
        int exiting = 0;
        Interlocked.CompareExchange(ref exiting, 1, 0);
        bool secondAttempt = Interlocked.CompareExchange(ref exiting, 1, 0) == 0;

        Assert.False(secondAttempt);
        Assert.Equal(1, exiting);
    }

    [Fact]
    public void ExitGuard_ConcurrentCalls_OnlyFirstSucceeds()
    {
        int exiting = 0;
        int successCount = 0;

        Parallel.For(0, 10, _ =>
        {
            if (Interlocked.CompareExchange(ref exiting, 1, 0) == 0)
            {
                Interlocked.Increment(ref successCount);
            }
        });

        Assert.Equal(1, successCount);
        Assert.Equal(1, exiting);
    }

    [Fact]
    public async Task ExitGuard_HighContention_StillOnlyOneWinner()
    {
        int exiting = 0;
        int successCount = 0;
        const int threadCount = 100;

        var tasks = new Task[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                if (Interlocked.CompareExchange(ref exiting, 1, 0) == 0)
                {
                    Interlocked.Increment(ref successCount);
                }
            });
        }

        await Task.WhenAll(tasks);

        Assert.Equal(1, successCount);
        Assert.Equal(1, exiting);
    }

    [Fact]
    public void ExitGuard_RapidSequentialCalls_OnlyFirstSucceeds()
    {
        int exiting = 0;
        int attemptCount = 0;
        int successCount = 0;

        for (int i = 0; i < 100; i++)
        {
            attemptCount++;
            if (Interlocked.CompareExchange(ref exiting, 1, 0) == 0)
            {
                successCount++;
            }
        }

        Assert.Equal(1, successCount);
        Assert.Equal(100, attemptCount);
    }

    [Fact]
    public void ExitGuard_StatePreservedAfterExit()
    {
        int exiting = 0;
        int firstResult = Interlocked.CompareExchange(ref exiting, 1, 0);
        int secondResult = Interlocked.CompareExchange(ref exiting, 1, 0);

        Assert.Equal(0, firstResult);
        Assert.Equal(1, secondResult);
        Assert.Equal(1, exiting);
    }

    [Fact]
    public async Task ExitGuard_ThreadSafety_AtomicOperation()
    {
        int exiting = 0;
        int[] results = new int[10];

        Task[] tasks = new Task[10];
        for (int i = 0; i < 10; i++)
        {
            int index = i;
            tasks[i] = Task.Run(() =>
            {
                results[index] = Interlocked.CompareExchange(ref exiting, 1, 0);
            });
        }

        await Task.WhenAll(tasks);

        // Exactly one thread should get 0 (success), rest should get 1 (already exited)
        int zeroCount = Array.FindAll(results, x => x == 0).Length;
        int oneCount = Array.FindAll(results, x => x == 1).Length;

        Assert.Equal(1, zeroCount);
        Assert.Equal(9, oneCount);
    }

    [Fact]
    public void ExitGuard_Pattern_CanBeUsedMultipleTimes()
    {
        // Simulate multiple screensaver instances with independent exit guards
        int exit1 = 0, exit2 = 0, exit3 = 0;

        bool result1 = Interlocked.CompareExchange(ref exit1, 1, 0) == 0;
        bool result2 = Interlocked.CompareExchange(ref exit2, 1, 0) == 0;
        bool result3 = Interlocked.CompareExchange(ref exit3, 1, 0) == 0;

        Assert.True(result1);
        Assert.True(result2);
        Assert.True(result3);

        bool result1Again = Interlocked.CompareExchange(ref exit1, 1, 0) == 0;
        bool result2Again = Interlocked.CompareExchange(ref exit2, 1, 0) == 0;
        bool result3Again = Interlocked.CompareExchange(ref exit3, 1, 0) == 0;

        Assert.False(result1Again);
        Assert.False(result2Again);
        Assert.False(result3Again);
    }

    [Fact]
    public void ExitGuard_NoRaceCondition_UnderStress()
    {
        int exiting = 0;
        int exitCount = 0;

        Parallel.For(0, 1000, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            _ =>
            {
                if (Interlocked.CompareExchange(ref exiting, 1, 0) == 0)
                {
                    Interlocked.Increment(ref exitCount);
                }
            });

        Assert.Equal(1, exitCount);
    }
}
