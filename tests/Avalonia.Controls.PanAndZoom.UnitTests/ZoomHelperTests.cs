using Xunit;

namespace Avalonia.Controls.PanAndZoom.UnitTests
{
    public class ZoomHelperTests
    {
        private static Matrix CreateMatrix(double scaleX = 1.0, double scaleY = 1.0, double offsetX = 0.0, double offsetY = 0.0)
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
            var bounds = new Rect(0, 0, 300, 300);
            var matrix = CreateMatrix(offsetX: -100);
            ZoomHelper.CalculateScrollable(bounds, matrix, out var extent, out var viewport, out var  offset);
            Assert.Equal(new Size(400, 300), extent);
            Assert.Equal(new Size(300, 300), viewport);
            Assert.Equal(new Vector(100, 0), offset);
        }
        
        [Fact]
        public void CalculateScrollable_OffsetX_Positive()
        {
            var bounds = new Rect(0, 0, 300, 300);
            var matrix = CreateMatrix(offsetX: 100);
            ZoomHelper.CalculateScrollable(bounds, matrix, out var extent, out var viewport, out var  offset);
            Assert.Equal(new Size(400, 300), extent);
            Assert.Equal(new Size(300, 300), viewport);
            Assert.Equal(new Vector(0, 0), offset);
        }
        
        [Fact]
        public void CalculateScrollable_OffsetY_Negative()
        {
            var bounds = new Rect(0, 0, 300, 300);
            var matrix = CreateMatrix(offsetY: -100);
            ZoomHelper.CalculateScrollable(bounds, matrix, out var extent, out var viewport, out var  offset);
            Assert.Equal(new Size(300, 400), extent);
            Assert.Equal(new Size(300, 300), viewport);
            Assert.Equal(new Vector(0, 100), offset);
        }
        
        [Fact]
        public void CalculateScrollable_OffsetY_Positive()
        {
            var bounds = new Rect(0, 0, 300, 300);
            var matrix = CreateMatrix(offsetY: 100);
            ZoomHelper.CalculateScrollable(bounds, matrix, out var extent, out var viewport, out var  offset);
            Assert.Equal(new Size(300, 400), extent);
            Assert.Equal(new Size(300, 300), viewport);
            Assert.Equal(new Vector(0, 0), offset);
        }
        
        [Fact]
        public void CalculateScrollable_ZoomIn_2x()
        {
            var bounds = new Rect(0, 0, 300, 300);
            var matrix = CreateMatrix(scaleX: 2, scaleY: 2);
            ZoomHelper.CalculateScrollable(bounds, matrix, out var extent, out var viewport, out var  offset);
            Assert.Equal(new Size(600, 600), extent);
            Assert.Equal(new Size(300, 300), viewport);
            Assert.Equal(new Vector(0, 0), offset);
        }
        
        [Fact]
        public void CalculateScrollable_ZoomOut_0_5x()
        {
            var bounds = new Rect(0, 0, 300, 300);
            var matrix = CreateMatrix(scaleX: 0.5, scaleY: 0.5);
            ZoomHelper.CalculateScrollable(bounds, matrix, out var extent, out var viewport, out var  offset);
            Assert.Equal(new Size(300, 300), extent);
            Assert.Equal(new Size(300, 300), viewport);
            Assert.Equal(new Vector(0, 0), offset);
        }
    }
}
