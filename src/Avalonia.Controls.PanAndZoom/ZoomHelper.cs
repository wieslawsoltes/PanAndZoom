using System;
using System.Diagnostics;
using static System.Math;

namespace Avalonia.Controls.PanAndZoom
{
    /// <summary>
    /// Zoom helper methods.
    /// </summary>
    public static class ZoomHelper
    {
        /// <summary>
        /// Calculate scrollable properties.
        /// </summary>
        /// <param name="bounds">The view bounds.</param>
        /// <param name="matrix">The transform matrix.</param>
        /// <param name="extent">The extent of the scrollable content.</param>
        /// <param name="viewport">The size of the viewport.</param>
        /// <param name="offset">The current scroll offset.</param>
        public static void CalculateScrollable(Rect bounds, Matrix matrix, out Size extent, out Size viewport, out Vector offset)
        {
            var transformed = bounds.TransformToAABB(matrix);

            Debug.WriteLine($"bounds: {bounds}, transformed: {transformed}");
            
            var width = transformed.Size.Width;
            var height = transformed.Size.Height;

            if (width < bounds.Width)
            {
                width = bounds.Width;

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
            else
            {
                width += Abs(transformed.Position.X);
            }
            
            if (height < bounds.Height)
            {
                height = bounds.Height;
                
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
            else
            {
                height += Abs(transformed.Position.Y);
            }

            extent = new Size(width, height);

            viewport = bounds.Size;
     
            var ox = transformed.Position.X;
            var oy = transformed.Position.Y;

            var offsetX = ox < 0 ? Abs(ox) : 0;
            var offsetY = oy < 0 ? Abs(oy) : 0;

            offset = new Vector(offsetX, offsetY);

            Debug.WriteLine($"Extent: {extent} | Offset: {offset} | Viewport: {viewport}");

        }
    }
}
