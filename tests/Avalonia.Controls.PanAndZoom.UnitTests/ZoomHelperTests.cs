using Xunit;

namespace Avalonia.Controls.PanAndZoom.UnitTests
{
    public class ZoomHelperTests
    {
        private Matrix CreateMatrix(double scaleX = 1.0, double scaleY = 1.0, double offsetX = 0.0, double offsetY = 0.0)
        {
            return new Matrix(scaleX, 0, 0, scaleY, offsetX, offsetY);
        }
        
        [Fact]
        public void CalculateScrollable_Default()
        {
            var bounds = new Rect(0, 0, 100, 100);
            var matrix = CreateMatrix();
            ZoomHelper.CalculateScrollable(bounds, matrix, out var extent, out var viewport, out var  offset);
            Assert.Equal(new Size(100, 100), extent);
            Assert.Equal(new Size(100, 100), viewport);
            Assert.Equal(new Vector(0, 0), offset);
        }
    }
}
