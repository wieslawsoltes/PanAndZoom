// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core;

/// <summary>
/// A UI framework independent thickness (padding or margin).
/// </summary>
/// <param name="Left">The left thickness.</param>
/// <param name="Top">The top thickness.</param>
/// <param name="Right">The right thickness.</param>
/// <param name="Bottom">The bottom thickness.</param>
public readonly record struct CoreThickness(double Left, double Top, double Right, double Bottom)
{
    /// <summary>
    /// Initializes a uniform thickness.
    /// </summary>
    /// <param name="uniform">The uniform thickness applied to all sides.</param>
    public CoreThickness(double uniform) : this(uniform, uniform, uniform, uniform)
    {
    }
}
