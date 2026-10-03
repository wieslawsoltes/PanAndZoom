// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
#if PANANDZOOM_WINUI
using System.Diagnostics;
using Microsoft.UI.Dispatching;
using Windows.UI.Input.Preview.Injection;

namespace PanAndZoom.Uno.RuntimeTests.Infrastructure;

/// <summary>
/// Paces real Windows input injection for the native WinUI test app.
/// </summary>
/// <remarks>
/// <para>
/// Uno processes injected input synchronously. Windows dispatches it asynchronously through the OS input
/// queue, coalesces pointer updates that arrive in a burst and cancels touch contacts that are not refreshed.
/// To behave like real hardware, injections are queued and fed to Windows one frame per system timer tick
/// (in order, mouse, keyboard, touch and pen alike). Every touch frame describes all active contacts (Windows
/// lifts contacts missing from a frame) and active contacts are refreshed while the queue is idle.
/// </para>
/// <para>
/// <see cref="ZoomBorderTestHelper.WaitForIdleAsync"/> waits for the queue to drain, so test code is the same on
/// Uno and WinUI.
/// </para>
/// </remarks>
internal static class WinUIInputPump
{
    private static readonly Queue<Action> s_queue = new();
    private static readonly Dictionary<uint, InjectedInputTouchInfo> s_touchContacts = new();
    private static readonly Dictionary<uint, InjectedInputPoint> s_postedLocations = new();
    private static readonly HashSet<uint> s_postedMovedContacts = new();
    private static readonly List<TaskCompletionSource> s_drainWaiters = new();
    private static DispatcherQueueTimer? s_timer;
    private static int s_idleTicks;
    private static long s_resumeAt;

    // Windows timers tick at the system timer resolution (15.6 ms by default): an interval below it
    // fires every system tick, while a 16 ms interval would fire every other tick (31 ms).
    private static readonly TimeSpan s_timerInterval = TimeSpan.FromMilliseconds(10);

    /// <summary>
    /// Queues an injection.
    /// </summary>
    public static void Post(Action injection)
    {
        s_queue.Enqueue(injection);
        EnsureTimer();
    }

    /// <summary>
    /// Gets a value indicating whether all queued injections were injected.
    /// </summary>
    public static bool IsIdle => s_queue.Count == 0;

    /// <summary>
    /// Queues a pause of <paramref name="milliseconds"/> before the next injection.
    /// </summary>
    /// <remarks>
    /// Windows ignores the injected time offsets (they must be 0 for touch), so a time offset longer than a
    /// frame, for example a held tap, becomes a real delay. Active touch contacts are kept alive meanwhile.
    /// </remarks>
    public static void PostDelay(int milliseconds)
    {
        if (milliseconds <= 20)
        {
            return;
        }

        Post(() => s_resumeAt = Stopwatch.GetTimestamp() + (long)(milliseconds * Stopwatch.Frequency / 1000.0));
    }

    /// <summary>
    /// Forgets the active touch contacts (touch injection was uninitialized, which cancels them).
    /// </summary>
    public static void ForgetTouchContacts()
    {
        s_touchContacts.Clear();
    }

    /// <summary>
    /// Queues a touch frame (merged with the active contacts when it is injected).
    /// </summary>
    /// <remarks>
    /// A real finger is never perfectly still: Windows holds back the PointerPressed of stationary contacts
    /// (two stationary contacts are never delivered), so each new contact is nudged by one pixel and back.
    /// The manipulation output of Windows trails a moving finger by a few pixels, so a contact that moved is
    /// held still for a few frames before it is lifted, like a finger that stops and then lifts.
    /// </remarks>
    public static void PostTouch(IReadOnlyList<InjectedInputTouchInfo> frame)
    {
        // Hold contacts that moved still before they are lifted.
        var liftMovedContact = false;
        foreach (var info in frame)
        {
            var id = info.PointerInfo.PointerId;
            var location = info.PointerInfo.PixelLocation;
            var options = info.PointerInfo.PointerOptions;
            if ((options & (InjectedInputPointerOptions.PointerUp | InjectedInputPointerOptions.Canceled)) != 0)
            {
                liftMovedContact |= s_postedMovedContacts.Remove(id);
                s_postedLocations.Remove(id);
            }
            else
            {
                if ((options & InjectedInputPointerOptions.PointerDown) == 0
                    && s_postedLocations.TryGetValue(id, out var previous)
                    && (previous.PositionX != location.PositionX || previous.PositionY != location.PositionY))
                {
                    s_postedMovedContacts.Add(id);
                }

                s_postedLocations[id] = location;
            }
        }

        if (liftMovedContact)
        {
            for (var i = 0; i < HoldFramesBeforeLift; i++)
            {
                Post(InjectTouchContactsNow);
            }
        }

        Post(() => InjectTouchNow(frame));

        var pressed = frame.Where(info => (info.PointerInfo.PointerOptions & InjectedInputPointerOptions.PointerDown) != 0).ToList();
        if (pressed.Count > 0)
        {
            Post(() => InjectTouchNow(pressed.Select(info => Nudge(info, 1)).ToList(), isNudge: true));
            Post(() => InjectTouchNow(pressed.Select(info => Nudge(info, 0)).ToList(), isNudge: true));
        }
    }

    private const int HoldFramesBeforeLift = 6;

    private static InjectedInputTouchInfo Nudge(InjectedInputTouchInfo info, int dx)
    {
        var update = AsUpdate(info);
        var pointerInfo = update.PointerInfo;
        pointerInfo.PixelLocation = new InjectedInputPoint { PositionX = info.PointerInfo.PixelLocation.PositionX + dx, PositionY = info.PointerInfo.PixelLocation.PositionY };
        update.PointerInfo = pointerInfo;
        return update;
    }

    /// <summary>
    /// Queues lifting every active touch contact.
    /// </summary>
    public static void PostLiftTouchContacts()
    {
        Post(LiftTouchContactsNow);
    }

    /// <summary>
    /// Waits until every queued injection was injected.
    /// </summary>
    public static Task WhenDrainedAsync()
    {
        if (s_queue.Count == 0)
        {
            return Task.CompletedTask;
        }

        var waiter = new TaskCompletionSource();
        s_drainWaiters.Add(waiter);
        return waiter.Task;
    }

    /// <summary>
    /// Drops the queued injections and lifts the active contacts immediately (test cleanup).
    /// </summary>
    public static void Reset()
    {
        s_queue.Clear();
        s_resumeAt = 0;
        s_postedLocations.Clear();
        s_postedMovedContacts.Clear();
        LiftTouchContactsNow();
        CompleteDrainWaiters();
    }

    private static void EnsureTimer()
    {
        if (s_timer == null)
        {
            var dispatcherQueue = DispatcherQueue.GetForCurrentThread()
                ?? throw new InvalidOperationException("Input must be injected from the UI thread.");
            s_timer = dispatcherQueue.CreateTimer();
            s_timer.Interval = s_timerInterval;
            s_timer.IsRepeating = true;
            s_timer.Tick += (_, _) => OnTick();
        }

        if (!s_timer.IsRunning)
        {
            s_timer.Start();
        }
    }

    private static void OnTick()
    {
        if (s_queue.Count > 0 && Stopwatch.GetTimestamp() >= s_resumeAt)
        {
            s_idleTicks = 0;
            var injection = s_queue.Dequeue();
            try
            {
                injection();
            }
            catch (Exception ex)
            {
                WinUITestRunnerLog($"Input injection failed: {ex.Message}");
            }

            if (s_queue.Count == 0)
            {
                CompleteDrainWaiters();
            }

            return;
        }

        if (s_touchContacts.Count > 0 || s_queue.Count > 0)
        {
            // Keep the active contacts alive (Windows cancels contacts that are not refreshed).
            if (++s_idleTicks % 3 == 0)
            {
                try
                {
                    InjectTouchContactsNow();
                }
                catch (Exception)
                {
                    s_touchContacts.Clear();
                }
            }

            return;
        }

        s_timer?.Stop();
    }

    private static void CompleteDrainWaiters()
    {
        var waiters = s_drainWaiters.ToArray();
        s_drainWaiters.Clear();
        foreach (var waiter in waiters)
        {
            waiter.TrySetResult();
        }
    }

    private static void InjectTouchContactsNow()
    {
        if (s_touchContacts.Count > 0)
        {
            InputHelper.RawInjector.InjectTouchInput(s_touchContacts.Values.Select(AsUpdate).ToList());
        }
    }

    private static void InjectTouchNow(IReadOnlyList<InjectedInputTouchInfo> frame, bool isNudge = false)
    {
        if (isNudge)
        {
            // Contacts lifted meanwhile are not nudged.
            frame = frame.Where(info => s_touchContacts.ContainsKey(info.PointerInfo.PointerId)).ToList();
            if (frame.Count == 0)
            {
                return;
            }
        }

        InputHelper.EnsureTouchInjectionNow();

        var givenIds = frame.Select(info => info.PointerInfo.PointerId).ToHashSet();
        var full = new List<InjectedInputTouchInfo>(frame);
        full.AddRange(s_touchContacts.Where(pair => !givenIds.Contains(pair.Key)).Select(pair => AsUpdate(pair.Value)));

        foreach (var info in frame)
        {
            var options = info.PointerInfo.PointerOptions;
            var id = info.PointerInfo.PointerId;
            if ((options & (InjectedInputPointerOptions.PointerUp | InjectedInputPointerOptions.Canceled)) != 0)
            {
                s_touchContacts.Remove(id);
            }
            else
            {
                s_touchContacts[id] = info;
            }
        }

        InputHelper.RawInjector.InjectTouchInput(full);
    }

    private static void LiftTouchContactsNow()
    {
        if (s_touchContacts.Count == 0)
        {
            return;
        }

        var up = s_touchContacts.Values.Select(info =>
        {
            var pointerInfo = info.PointerInfo;
            pointerInfo.PointerOptions = InjectedInputPointerOptions.PointerUp | (pointerInfo.PointerOptions & InjectedInputPointerOptions.FirstButton);
            pointerInfo.TimeOffsetInMilliseconds = 0;
            return new InjectedInputTouchInfo { PointerInfo = pointerInfo, Contact = info.Contact, Pressure = info.Pressure, TouchParameters = info.TouchParameters };
        }).ToList();

        ForgetTouchContacts();
        try
        {
            InputHelper.RawInjector.InjectTouchInput(up);
        }
        catch (Exception)
        {
            // Best effort.
        }
    }

    private static InjectedInputTouchInfo AsUpdate(InjectedInputTouchInfo info)
    {
        var pointerInfo = info.PointerInfo;
        pointerInfo.PointerOptions = InjectedInputPointerOptions.Update
            | InjectedInputPointerOptions.InContact
            | InjectedInputPointerOptions.InRange
            | (pointerInfo.PointerOptions & InjectedInputPointerOptions.FirstButton);
        pointerInfo.TimeOffsetInMilliseconds = 0;

        return new InjectedInputTouchInfo
        {
            PointerInfo = pointerInfo,
            Contact = info.Contact,
            Pressure = info.Pressure,
            Orientation = info.Orientation,
            TouchParameters = info.TouchParameters
        };
    }

    private static void WinUITestRunnerLog(string message) => PanAndZoom.Uno.RuntimeTests.WinUITestRunner.Log(message);
}
#endif
