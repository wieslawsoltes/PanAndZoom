// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using static System.Math;

namespace Avalonia.Controls.PanAndZoom;

/// <summary>
/// Pan and zoom control for Avalonia.
/// </summary>
public partial class ZoomBorder : ILogicalScrollable
{
    private Size _extent;
    private Size _viewport;
    private Vector _offset;
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
        viewport = borderSize;

        var transformed = TransformContentToViewport(contentBounds, layoutOffset, matrix);
        
        Log($"[CalculateScrollable] contentBounds: {contentBounds}, layoutOffset: {layoutOffset}, transformed: {transformed}");

        var width = transformed.Size.Width;
        var height = transformed.Size.Height;

        if (width < viewport.Width)
        {
            width = viewport.Width;

            if (transformed.Position.X < 0.0)
            {
                width += Abs(transformed.Position.X);
            }
            else
            {
                var widthTranslated = transformed.Size.Width + transformed.Position.X;
                if (widthTranslated > width)
                {
                    width += widthTranslated - width;
                }
            }
        }
        else if (!(width > viewport.Width))
        {
            width += Abs(transformed.Position.X);
        }
            
        if (height < viewport.Height)
        {
            height = viewport.Height;
                
            if (transformed.Position.Y < 0.0)
            {
                height += Abs(transformed.Position.Y);
            }
            else
            {
                var heightTranslated = transformed.Size.Height + transformed.Position.Y;
                if (heightTranslated > height)
                {
                    height += heightTranslated - height;
                }
            }
        }
        else if (!(height > viewport.Height))
        {
            height += Abs(transformed.Position.Y);
        }

        extent = new Size(width, height);

        var ox = transformed.Position.X;
        var oy = transformed.Position.Y;

        var offsetX = ox < 0 ? Abs(ox) : 0;
        var offsetY = oy < 0 ? Abs(oy) : 0;

        offset = new Vector(offsetX, offsetY);

        Log($"[CalculateScrollable] Extent: {extent} | Offset: {offset} | Viewport: {viewport}");
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
        var transformedContent = contentRect.TransformToAABB(matrix);
        
        return new Rect(
            transformedContent.X + layoutOffset.X,
            transformedContent.Y + layoutOffset.Y,
            transformedContent.Width,
            transformedContent.Height);
    }

    /// <inheritdoc/>
    Size IScrollable.Extent => _extent;

    /// <inheritdoc/>
    Vector IScrollable.Offset
    {
        get => _offset;
        set
        {
            Log($"[Offset] offset value: {value}");
            if (_updating)
            {
                return;
            }
            _updating = true;

            var (x, y) = _offset;
            var dx = x - value.X;
            var dy = y - value.Y;

            _offset = value;

            Log($"[Offset] offset: {_offset}, dx: {dx}, dy: {dy}");

            if (dx != 0 || dy != 0)
            {
                _matrix = MatrixHelper.ScaleAndTranslate(_zoomX, _zoomY, _matrix.M31 + dx, _matrix.M32 + dy);
                Invalidate(!this.IsPointerOver);
            }

            _updating = false;
        }
    }

    /// <inheritdoc/>
    Size IScrollable.Viewport => _viewport;

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
            // target.Bounds.Position is the layout offset, and TransformContentToViewport
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

        // Account for the layout offset of _element within ZoomBorder
        // and transform the target bounds to viewport coordinates
        var adjustedBounds = TransformContentToViewport(targetBounds, LayoutOffset, _matrix);

        // Get current viewport
        var viewportRect = new Rect(0, 0, Bounds.Width, Bounds.Height);

        // Check if already fully visible
        if (viewportRect.Contains(adjustedBounds))
        {
            return true;
        }

        // Calculate required pan to bring target into view
        var deltaX = 0.0;
        var deltaY = 0.0;

        // Check horizontal visibility
        if (adjustedBounds.Left < viewportRect.Left)
        {
            deltaX = viewportRect.Left - adjustedBounds.Left;
        }
        else if (adjustedBounds.Right > viewportRect.Right)
        {
            deltaX = viewportRect.Right - adjustedBounds.Right;
        }

        // Check vertical visibility
        if (adjustedBounds.Top < viewportRect.Top)
        {
            deltaY = viewportRect.Top - adjustedBounds.Top;
        }
        else if (adjustedBounds.Bottom > viewportRect.Bottom)
        {
            deltaY = viewportRect.Bottom - adjustedBounds.Bottom;
        }

        // Apply pan if needed
        if (deltaX != 0 || deltaY != 0)
        {
            PanDelta(deltaX, deltaY);
        }

        return true;
    }

    Control? ILogicalScrollable.GetControlInDirection(NavigationDirection direction, Control? from)
    {
        return null;
    }

    void ILogicalScrollable.RaiseScrollInvalidated(EventArgs e)
    {
        _scrollInvalidated?.Invoke(this, e);
    }

    private void InvalidateScrollable()
    {
        if (this is not ILogicalScrollable scrollable)
        {
            return;
        }

        if (_element == null)
        {
            return;
        }

        var contentBounds = BoundsMode == ContentBoundsMode.Custom
            ? GetContentBounds()
            : new Rect(0, 0, _element.Bounds.Width, _element.Bounds.Height);

        CalculateScrollable(contentBounds, LayoutOffset, Bounds.Size, _matrix, out var extent, out var viewport, out var offset);

        Log($"[InvalidateScrollable] _element.Bounds: {_element.Bounds}, _matrix: {_matrix}");
        Log($"[InvalidateScrollable] _extent: {_extent}, extent: {extent}, diff: {extent - _extent}");
        Log($"[InvalidateScrollable] _offset: {_offset}, offset: {offset}, diff: {offset - _offset}");
        Log($"[InvalidateScrollable] _viewport: {_viewport}, viewport: {viewport}, diff: {viewport - _viewport}");

        _extent = extent;
        _offset = offset;
        _viewport = viewport;

        // Temporarily set updating flag to prevent feedback loop when ScrollViewer
        // reads the new offset and sets it back through IScrollable.Offset setter
        var wasUpdating = _updating;
        _updating = true;
        
        scrollable.RaiseScrollInvalidated(EventArgs.Empty);
        
        _updating = wasUpdating;
    }
}
