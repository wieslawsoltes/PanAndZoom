using System;

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

            var x = transformed.Position.X;
            var y = transformed.Position.Y;

            extent = new Size(width + Math.Abs(x), height + Math.Abs(y));

            var offsetX = x < 0 ? extent.Width - x : 0;
            var offsetY = y < 0 ? extent.Height - y : 0;

            offsetX -= width - bounds.Width;
            offsetY -= height - bounds.Height;

            offset = new Vector(offsetX, offsetY);

            viewport = bounds.Size;
        }
    }
}
