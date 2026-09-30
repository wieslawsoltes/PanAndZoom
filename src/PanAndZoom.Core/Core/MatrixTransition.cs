// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;

namespace PanAndZoom.Core;

/// <summary>
/// A UI framework independent linear transition between two transform matrices.
/// </summary>
/// <remarks>
/// Hosts without a built-in transform transition system (for example Uno Platform) drive the
/// transition from a render loop by calling <see cref="GetCurrentValue"/> with a monotonic timestamp.
/// </remarks>
public sealed class MatrixTransition
{
    private CoreMatrix _from = CoreMatrix.Identity;
    private CoreMatrix _to = CoreMatrix.Identity;
    private TimeSpan _start;
    private TimeSpan _duration;

    /// <summary>
    /// Gets a value indicating whether the transition is running.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Gets the transition start value.
    /// </summary>
    public CoreMatrix From => _from;

    /// <summary>
    /// Gets the transition target value.
    /// </summary>
    public CoreMatrix To => _to;

    /// <summary>
    /// Gets the transition duration.
    /// </summary>
    public TimeSpan Duration => _duration;

    /// <summary>
    /// Starts a new transition.
    /// </summary>
    /// <param name="from">The start value.</param>
    /// <param name="to">The target value.</param>
    /// <param name="duration">The transition duration.</param>
    /// <param name="now">The current monotonic timestamp.</param>
    public void Start(CoreMatrix from, CoreMatrix to, TimeSpan duration, TimeSpan now)
    {
        _from = from;
        _to = to;
        _duration = duration;
        _start = now;
        IsRunning = duration > TimeSpan.Zero && from != to;
    }

    /// <summary>
    /// Stops the transition.
    /// </summary>
    public void Stop()
    {
        IsRunning = false;
    }

    /// <summary>
    /// Gets the transition progress in the range [0, 1].
    /// </summary>
    /// <param name="now">The current monotonic timestamp.</param>
    /// <returns>The progress.</returns>
    public double GetProgress(TimeSpan now)
    {
        if (!IsRunning || _duration <= TimeSpan.Zero)
        {
            return 1.0;
        }

        var progress = (now - _start).TotalMilliseconds / _duration.TotalMilliseconds;
        return progress < 0.0 ? 0.0 : progress > 1.0 ? 1.0 : progress;
    }

    /// <summary>
    /// Gets the current interpolated value and stops the transition when it completes.
    /// </summary>
    /// <param name="now">The current monotonic timestamp.</param>
    /// <returns>The current value.</returns>
    public CoreMatrix GetCurrentValue(TimeSpan now)
    {
        if (!IsRunning)
        {
            return _to;
        }

        var progress = GetProgress(now);
        if (progress >= 1.0)
        {
            IsRunning = false;
            return _to;
        }

        return MatrixMath.Interpolate(_from, _to, progress);
    }
}
