// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;
using Microsoft.UI.Xaml;

namespace PanAndZoom;

/// <summary>
/// Logical scrolling support for <see cref="ZoomBorder"/>.
/// </summary>
/// <remarks>
/// WinUI has no logical scrolling contract equivalent to Avalonia's <c>ILogicalScrollable</c>, so the
/// logical scroll state (<see cref="Extent"/>, <see cref="Viewport"/>, <see cref="ScrollOffset"/>) is
/// exposed directly. It can be used to drive custom scroll bars or mini-maps.
/// </remarks>
public partial class ZoomBorder
{
    /// <summary>
    /// Calculate scrollable properties.
    /// </summary>
    /// <param name="source">The source bounds (layout position and size of the child element).</param>
    /// <param name="borderSize">The size of border (this control).</param>
    /// <param name="matrix">The transform matrix.</param>
    /// <param name="extent">The extent of the scrollable content.</param>
    /// <param name="viewport">The size of the viewport.</param>
    /// <param name="offset">The current scroll offset.</param>
    public static void CalculateScrollable(Rect source, Size borderSize, Matrix matrix, out Size extent, out Size viewport, out Point offset)
    {
        var layoutOffset = new Point(source.X, source.Y);
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
        out Point offset)
    {
        PanAndZoomEngine.CalculateScrollable(
            contentBounds.ToCore(),
            layoutOffset.ToCore(),
            borderSize.ToCore(),
            matrix.ToCore(),
            out var coreExtent,
            out var coreViewport,
            out var coreOffset);

        extent = coreExtent.ToUno();
        viewport = coreViewport.ToUno();
        offset = coreOffset.ToUno();
    }

    /// <summary>
    /// Transforms content bounds to viewport coordinates, accounting for the layout offset in <paramref name="elementBounds"/>.
    /// </summary>
    /// <param name="elementBounds">The element bounds (includes the layout position of the element).</param>
    /// <param name="matrix">The transform matrix.</param>
    /// <returns>The visual bounds in viewport coordinates.</returns>
    public static Rect TransformContentToViewport(Rect elementBounds, Matrix matrix)
    {
        var layoutOffset = new Point(elementBounds.X, elementBounds.Y);
        var contentBounds = new Rect(0, 0, elementBounds.Width, elementBounds.Height);
        return TransformContentToViewport(contentBounds, layoutOffset, matrix);
    }

    /// <summary>
    /// Transforms a rectangle in content coordinates to viewport coordinates, accounting for layout offset.
    /// </summary>
    /// <param name="contentRect">A rectangle in content coordinates (where 0,0 is the top-left of the content).</param>
    /// <param name="layoutOffset">The layout offset where the element is positioned within its parent.</param>
    /// <param name="matrix">The transform matrix.</param>
    /// <returns>The rectangle in viewport coordinates.</returns>
    public static Rect TransformContentToViewport(Rect contentRect, Point layoutOffset, Matrix matrix)
    {
        return PanAndZoomEngine.TransformContentToViewport(contentRect.ToCore(), layoutOffset.ToCore(), matrix.ToCore()).ToUno();
    }

    /// <summary>
    /// Raised when the logical scroll state (<see cref="Extent"/>, <see cref="Viewport"/>, <see cref="ScrollOffset"/>) changes.
    /// </summary>
    public event EventHandler? ScrollInvalidated;

    /// <summary>
    /// Gets the logical scroll extent.
    /// </summary>
    public Size Extent => _engine.Extent.ToUno();

    /// <summary>
    /// Gets the logical scroll viewport size.
    /// </summary>
    public Size Viewport => _engine.Viewport.ToUno();

    /// <summary>
    /// Gets or sets the logical scroll offset. Setting the offset pans the content by the offset difference.
    /// </summary>
    public Point ScrollOffset
    {
        get => _engine.ScrollOffset.ToUno();
        set => _engine.SetScrollOffset(value.ToCoreVector(), skipTransitions: true);
    }

    /// <summary>
    /// Gets or sets whether horizontal scrolling is enabled for the logical scroll contract.
    /// </summary>
    public bool CanHorizontallyScroll { get; set; }

    /// <summary>
    /// Gets or sets whether vertical scrolling is enabled for the logical scroll contract.
    /// </summary>
    public bool CanVerticallyScroll { get; set; }

    /// <summary>
    /// Pans the content so that the target element (a descendant of the child) becomes fully visible.
    /// </summary>
    /// <param name="target">The target element.</param>
    /// <param name="targetRect">The target rectangle in <paramref name="target"/> coordinates, or an empty rectangle to use the whole element.</param>
    /// <returns>True when the target is (or was made) visible.</returns>
    public bool BringIntoView(FrameworkElement target, Rect targetRect)
    {
        if (_element == null || target == null)
        {
            return false;
        }

        var targetBounds = targetRect;

        // If targetRect has zero size, use the target's size
        if (targetRect.IsEmpty || (targetRect.Width <= 0 && targetRect.Height <= 0))
        {
            targetBounds = new Rect(0, 0, target.ActualWidth, target.ActualHeight);
        }

        // Translate target coordinates to the content coordinate system
        if (!ReferenceEquals(target, _element))
        {
            try
            {
                targetBounds = target.TransformToVisual(_element).TransformBounds(targetBounds);
            }
            catch (Exception)
            {
                // Cannot determine transform, fail gracefully
                return false;
            }
        }

        return _engine.BringIntoView(targetBounds.ToCore());
    }

    private void ZoomBorder_BringIntoViewRequested(UIElement sender, BringIntoViewRequestedEventArgs args)
    {
        if (args.Handled || _element == null || args.TargetElement is not FrameworkElement target)
        {
            return;
        }

        if (!ReferenceEquals(target, _element) && !IsDescendantOfElement(target))
        {
            return;
        }

        if (BringIntoView(target, args.TargetRect))
        {
            args.Handled = true;
        }
    }

    private bool IsDescendantOfElement(DependencyObject element)
    {
        var current = element;
        while (current != null)
        {
            if (ReferenceEquals(current, _element))
            {
                return true;
            }

            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }

        return false;
    }
}
