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
        
        [Fact]
        public void CalculateScrollable_OffsetX_Negative()
        {
            var bounds = new Rect(0, 0, 100, 100);
            var matrix = CreateMatrix(offsetX: -25);
            ZoomHelper.CalculateScrollable(bounds, matrix, out var extent, out var viewport, out var  offset);
            Assert.Equal(new Size(125, 100), extent);
            Assert.Equal(new Size(100, 100), viewport);
            Assert.Equal(new Vector(100, 0), offset);
        }
        
        [Fact]
        public void CalculateScrollable_OffsetX_Positive()
        {
            var bounds = new Rect(0, 0, 100, 100);
            var matrix = CreateMatrix(offsetX: 25);
            ZoomHelper.CalculateScrollable(bounds, matrix, out var extent, out var viewport, out var  offset);
            Assert.Equal(new Size(125, 100), extent);
            Assert.Equal(new Size(100, 100), viewport);
            Assert.Equal(new Vector(0, 0), offset);
        }
        
        [Fact]
        public void CalculateScrollable_OffsetY_Negative()
        {
            var bounds = new Rect(0, 0, 100, 100);
            var matrix = CreateMatrix(offsetY: -25);
            ZoomHelper.CalculateScrollable(bounds, matrix, out var extent, out var viewport, out var  offset);
            Assert.Equal(new Size(100, 125), extent);
            Assert.Equal(new Size(100, 100), viewport);
            Assert.Equal(new Vector(0, 100), offset);
        }
        
        [Fact]
        public void CalculateScrollable_OffsetY_Positive()
        {
            var bounds = new Rect(0, 0, 100, 100);
            var matrix = CreateMatrix(offsetY: 25);
            ZoomHelper.CalculateScrollable(bounds, matrix, out var extent, out var viewport, out var  offset);
            Assert.Equal(new Size(100, 125), extent);
            Assert.Equal(new Size(100, 100), viewport);
            Assert.Equal(new Vector(0, 0), offset);
        }
    }
}
