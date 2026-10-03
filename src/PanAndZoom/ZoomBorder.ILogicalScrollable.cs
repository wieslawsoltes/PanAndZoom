// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace Avalonia.Controls.PanAndZoom;

/// <summary>
/// Pan and zoom control for Avalonia.
/// </summary>
public partial class ZoomBorder : ILogicalScrollable
{
    private bool _canHorizontallyScroll;
    private bool _canVerticallyScroll;
    private EventHandler? _scrollInvalidated;

    /// <summary>
    /// Calculate scrollable properties.
    /// </summary>
    /// <param name="source">The source bounds.</param>
    /// <param name="borderSize">The size of border (this control)</param>
    /// <param name="matrix">The transform matrix.</param>
    /// <param name="extent">The extent of the scrollable content.</param>
    /// <param name="viewport">The size of the viewport.</param>
    /// <param name="offset">The current scroll offset.</param>
    public static void CalculateScrollable(Rect source, Size borderSize, Matrix matrix, out Size extent, out Size viewport, out Vector offset)
    {
        // The source.Position is the layout position where Avalonia placed the child element
        // (e.g., centered at (50,50)).
        // This layout offset should not be scaled by the zoom matrix - it's a fixed offset
        // in viewport coordinates, not content coordinates.
        //
        // The content itself is in a coordinate system from (0,0) to (Width,Height).
        // Only the content bounds get transformed by the matrix.
        var layoutOffset = source.Position;
        var contentBounds = new Rect(0, 0, source.Width, source.Height);
        CalculateScrollable(contentBounds, layoutOffset, borderSize, matrix, out extent, out viewport, out offset);
    }

    /// <summary>
    /// Calculate scrollable properties for content-coordinate bounds and a separate layout offset.
    /// </summary>
    /// <param name="contentBounds">The effective bounds in content coordinates.</param>
    /// <param name="layoutOffset">The layout position of the child inside the ZoomBorder.</param>
    /// <param name="borderSize">The size of the ZoomBorder viewport.</param>
    /// <param name="matrix">The transform matrix.</param>
    /// <param name="extent">The extent of the scrollable content.</param>
    /// <param name="viewport">The size of the viewport.</param>
    /// <param name="offset">The current scroll offset.</param>
    public static void CalculateScrollable(
        Rect contentBounds,
        Point layoutOffset,
        Size borderSize,
        Matrix matrix,
        out Size extent,
        out Size viewport,
        out Vector offset)
    {
        PanAndZoomEngine.CalculateScrollable(
            contentBounds.ToCore(),
            layoutOffset.ToCore(),
            borderSize.ToCore(),
            matrix.ToCore(),
            out var coreExtent,
            out var coreViewport,
            out var coreOffset);

        extent = coreExtent.ToAvalonia();
        viewport = coreViewport.ToAvalonia();
        offset = coreOffset.ToAvalonia();
    }

    /// <summary>
    /// Transforms content bounds to viewport coordinates, accounting for layout offset.
    /// </summary>
    /// <param name="elementBounds">The element bounds (includes Position where Avalonia laid out the element).</param>
    /// <param name="matrix">The transform matrix.</param>
    /// <returns>The actual visual bounds in viewport coordinates.</returns>
    /// <remarks>
    /// When a child element has a differnt size than the ZoomBorder, Avalonia's layout system
    /// positions it (e.g., centered) resulting in a non-zero Position in elementBounds.
    /// This layout offset is in viewport coordinates and should not be scaled by the
    /// transform matrix. This method correctly separates the two coordinate systems.
    /// </remarks>
    public static Rect TransformContentToViewport(Rect elementBounds, Matrix matrix)
    {
        var layoutOffset = elementBounds.Position;
        var contentBounds = new Rect(0, 0, elementBounds.Width, elementBounds.Height);
        return TransformContentToViewport(contentBounds, layoutOffset, matrix);
    }

    /// <summary>
    /// Transforms a rectangle in content coordinates to viewport coordinates, accounting for layout offset.
    /// </summary>
    /// <param name="contentRect">A rectangle in content coordinates (where 0,0 is the top-left of the content).</param>
    /// <param name="layoutOffset">The layout offset where Avalonia positioned the element within its parent.</param>
    /// <param name="matrix">The transform matrix.</param>
    /// <returns>The rectangle in viewport coordinates.</returns>
    public static Rect TransformContentToViewport(Rect contentRect, Point layoutOffset, Matrix matrix)
    {
        return PanAndZoomEngine.TransformContentToViewport(contentRect.ToCore(), layoutOffset.ToCore(), matrix.ToCore()).ToAvalonia();
    }

    /// <inheritdoc/>
    Size IScrollable.Extent => _engine.Extent.ToAvalonia();

    /// <inheritdoc/>
    Vector IScrollable.Offset
    {
        get => _engine.ScrollOffset.ToAvalonia();
        set => _engine.SetScrollOffset(value.ToCore(), !IsPointerOver);
    }

    /// <inheritdoc/>
    Size IScrollable.Viewport => _engine.Viewport.ToAvalonia();

    /// <summary>
    /// Gets or sets whether horizontal scrolling is enabled for the logical scroll contract.
    /// </summary>
    public bool CanHorizontallyScroll
    {
        get => _canHorizontallyScroll;
        set
        {
            _canHorizontallyScroll = value;
            InvalidateMeasure();
        }
    }

    /// <summary>
    /// Gets or sets whether vertical scrolling is enabled for the logical scroll contract.
    /// </summary>
    public bool CanVerticallyScroll
    {
        get => _canVerticallyScroll;
        set
        {
            _canVerticallyScroll = value;
            InvalidateMeasure();
        }
    }

    bool ILogicalScrollable.IsLogicalScrollEnabled => true;

    event EventHandler? ILogicalScrollable.ScrollInvalidated
    {
        add => _scrollInvalidated += value;
        remove => _scrollInvalidated -= value;
    }

    Size ILogicalScrollable.ScrollSize => new Size(1, 1);

    Size ILogicalScrollable.PageScrollSize => new Size(10, 10);

    bool ILogicalScrollable.BringIntoView(Control target, Rect targetRect)
    {
        if (_element == null || target == null)
        {
            return false;
        }

        // Get the bounds of the target control relative to the ZoomBorder's child element
        var targetBounds = targetRect;

        // If targetRect has zero size, use the target's bounds
        if (targetRect.Width <= 0 && targetRect.Height <= 0)
        {
            // For _element itself, use content-space bounds (origin 0,0) since
            // target.Bounds.Position is the layout offset, and the engine
            // will add it again. For other controls, keep target.Bounds as-is since
            // TransformToVisual below handles the coordinate conversion.
            targetBounds = target == _element
                ? new Rect(0, 0, target.Bounds.Width, target.Bounds.Height)
                : target.Bounds;
        }

        // Try to translate target coordinates to our content coordinate system
        if (target != _element)
        {
            var transform = target.TransformToVisual(_element);
            if (transform.HasValue)
            {
                targetBounds = targetBounds.TransformToAABB(transform.Value);
            }
            else
            {
                // Cannot determine transform, fail gracefully
                return false;
            }
        }

        return _engine.BringIntoView(targetBounds.ToCore());
    }

    Control? ILogicalScrollable.GetControlInDirection(NavigationDirection direction, Control? from)
    {
        return null;
    }

    void ILogicalScrollable.RaiseScrollInvalidated(EventArgs e)
    {
        _scrollInvalidated?.Invoke(this, e);
    }
}
