// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core;

/// <summary>
/// A UI framework independent 2D vector.
/// </summary>
/// <param name="X">The X component.</param>
/// <param name="Y">The Y component.</param>
public readonly record struct CoreVector(double X, double Y)
{
    /// <summary>
    /// Gets the length of the vector.
    /// </summary>
    public double Length => System.Math.Sqrt(X * X + Y * Y);
}
