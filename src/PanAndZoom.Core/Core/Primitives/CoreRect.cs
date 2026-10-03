// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core;

/// <summary>
/// A UI framework independent axis aligned rectangle.
/// </summary>
/// <param name="X">The X position.</param>
/// <param name="Y">The Y position.</param>
/// <param name="Width">The width.</param>
/// <param name="Height">The height.</param>
public readonly record struct CoreRect(double X, double Y, double Width, double Height)
{
    /// <summary>
    /// Initializes a rectangle from a position and a size.
    /// </summary>
    /// <param name="position">The top left position.</param>
    /// <param name="size">The size.</param>
    public CoreRect(CorePoint position, CoreSize size)
        : this(position.X, position.Y, size.Width, size.Height)
    {
    }

    /// <summary>
    /// Initializes a rectangle from the top left and bottom right points.
    /// </summary>
    /// <param name="topLeft">The top left point.</param>
    /// <param name="bottomRight">The bottom right point.</param>
    public CoreRect(CorePoint topLeft, CorePoint bottomRight)
        : this(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y)
    {
    }

    /// <summary>
    /// Gets the left position.
    /// </summary>
    public double Left => X;

    /// <summary>
    /// Gets the top position.
    /// </summary>
    public double Top => Y;

    /// <summary>
    /// Gets the right position.
    /// </summary>
    public double Right => X + Width;

    /// <summary>
    /// Gets the bottom position.
    /// </summary>
    public double Bottom => Y + Height;

    /// <summary>
    /// Gets the top left point.
    /// </summary>
    public CorePoint TopLeft => new(X, Y);

    /// <summary>
    /// Gets the top right point.
    /// </summary>
    public CorePoint TopRight => new(Right, Y);

    /// <summary>
    /// Gets the bottom right point.
    /// </summary>
    public CorePoint BottomRight => new(Right, Bottom);

    /// <summary>
    /// Gets the bottom left point.
    /// </summary>
    public CorePoint BottomLeft => new(X, Bottom);

    /// <summary>
    /// Gets the position (top left point).
    /// </summary>
    public CorePoint Position => new(X, Y);

    /// <summary>
    /// Gets the size.
    /// </summary>
    public CoreSize Size => new(Width, Height);

    /// <summary>
    /// Determines whether a point is inside the rectangle (edges inclusive).
    /// </summary>
    /// <param name="p">The point.</param>
    /// <returns>True if the point is inside the rectangle.</returns>
    public bool Contains(CorePoint p)
    {
        return p.X >= X && p.X <= X + Width && p.Y >= Y && p.Y <= Y + Height;
    }

    /// <summary>
    /// Determines whether the rectangle fully contains another rectangle.
    /// </summary>
    /// <param name="r">The other rectangle.</param>
    /// <returns>True if the other rectangle is fully contained.</returns>
    public bool Contains(CoreRect r)
    {
        return Contains(r.TopLeft) && Contains(r.BottomRight);
    }

    /// <summary>
    /// Determines whether the rectangle intersects with another rectangle.
    /// </summary>
    /// <param name="rect">The other rectangle.</param>
    /// <returns>True if the rectangles intersect.</returns>
    public bool Intersects(CoreRect rect)
    {
        return rect.X < Right && X < rect.Right && rect.Y < Bottom && Y < rect.Bottom;
    }

    /// <summary>
    /// Gets the intersection of two rectangles.
    /// </summary>
    /// <param name="rect">The other rectangle.</param>
    /// <returns>The intersection, or an empty rectangle when the rectangles do not intersect.</returns>
    public CoreRect Intersect(CoreRect rect)
    {
        var newLeft = rect.X > X ? rect.X : X;
        var newTop = rect.Y > Y ? rect.Y : Y;
        var newRight = rect.Right < Right ? rect.Right : Right;
        var newBottom = rect.Bottom < Bottom ? rect.Bottom : Bottom;

        if (newRight > newLeft && newBottom > newTop)
        {
            return new CoreRect(newLeft, newTop, newRight - newLeft, newBottom - newTop);
        }

        return default;
    }

    /// <summary>
    /// Returns the axis-aligned bounding box of the rectangle transformed by a matrix.
    /// </summary>
    /// <param name="matrix">The transform.</param>
    /// <returns>The bounding box.</returns>
    public CoreRect TransformToAABB(CoreMatrix matrix)
    {
        var p1 = matrix.Transform(TopLeft);
        var p2 = matrix.Transform(TopRight);
        var p3 = matrix.Transform(BottomRight);
        var p4 = matrix.Transform(BottomLeft);

        var left = double.MaxValue;
        var right = double.MinValue;
        var top = double.MaxValue;
        var bottom = double.MinValue;

        Accumulate(p1);
        Accumulate(p2);
        Accumulate(p3);
        Accumulate(p4);

        return new CoreRect(new CorePoint(left, top), new CorePoint(right, bottom));

        void Accumulate(CorePoint p)
        {
            if (p.X < left) left = p.X;
            if (p.X > right) right = p.X;
            if (p.Y < top) top = p.Y;
            if (p.Y > bottom) bottom = p.Y;
        }
    }
}
