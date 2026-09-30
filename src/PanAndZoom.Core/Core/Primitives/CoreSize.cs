// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core;

/// <summary>
/// A UI framework independent 2D size.
/// </summary>
/// <param name="Width">The width.</param>
/// <param name="Height">The height.</param>
public readonly record struct CoreSize(double Width, double Height);
