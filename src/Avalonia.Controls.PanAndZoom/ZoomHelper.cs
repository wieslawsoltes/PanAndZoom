using System;
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

            var width = transformed.Size.Width;
            var height = transformed.Size.Height;

            if (width < bounds.Width)
            {
                width = bounds.Width;
            }
            
            if (height < bounds.Height)
            {
                height = bounds.Height;
            }

            var ex = matrix.M31;
            var ey = matrix.M32;

            extent = new Size(
                width + Math.Abs(ex),
                height + Math.Abs(ey));

            viewport = bounds.Size;
     
            var ox = matrix.M31 * matrix.M11;
            var oy = matrix.M32 * matrix.M22;

            var offsetX = ox < 0 ? Abs(ox) : 0;
            var offsetY = oy < 0 ? Abs(oy) : 0;

            offset = new Vector(offsetX, offsetY);
        }
    }
}
