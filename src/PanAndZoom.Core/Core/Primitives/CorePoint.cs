// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core;

/// <summary>
/// A UI framework independent 2D point.
/// </summary>
/// <param name="X">The X coordinate.</param>
/// <param name="Y">The Y coordinate.</param>
public readonly record struct CorePoint(double X, double Y)
{
    /// <summary>
    /// Transforms the point by the provided matrix.
    /// </summary>
    /// <param name="matrix">The transform matrix.</param>
    /// <returns>The transformed point.</returns>
    public CorePoint Transform(CoreMatrix matrix) => matrix.Transform(this);
}
