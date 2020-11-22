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

            var x = matrix.M31;
            var y = matrix.M32;

            extent = new Size(
                width + Math.Abs(x),
                height + Math.Abs(y));

            viewport = bounds.Size;
     
            var offsetX = x < 0 ? Abs(x) : 0;
            var offsetY = y < 0 ? Abs(y) : 0;

            offset = new Vector(offsetX, offsetY);
        }
    }
}
