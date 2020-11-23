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
        public void CalculateMatrix_StretchMode_None()
        {
            var panelBounds = new Rect(0, 0, 300, 200);
            var elementBounds = new Rect(0, 0, 100, 100);
            var target = ZoomHelper.CalculateMatrix(panelBounds.Width, panelBounds.Height, elementBounds.Width, elementBounds.Height, StretchMode.None);
            Assert.Equal(1.0, target.M11);
            Assert.Equal(0.0, target.M12);
            Assert.Equal(0.0, target.M21);
            Assert.Equal(1.0, target.M22);
            Assert.Equal(0.0, target.M31);
            Assert.Equal(0.0, target.M32);
        }

        [Fact]
        public void CalculateMatrix_StretchMode_Fill()
        {
            var panelBounds = new Rect(0, 0, 300, 200);
            var elementBounds = new Rect(0, 0, 100, 100);
            var target = ZoomHelper.CalculateMatrix(panelBounds.Width, panelBounds.Height, elementBounds.Width, elementBounds.Height, StretchMode.Fill);
            Assert.Equal(3.0, target.M11);
            Assert.Equal(0.0, target.M12);
            Assert.Equal(0.0, target.M21);
            Assert.Equal(2.0, target.M22);
            Assert.Equal(-100.0, target.M31);
            Assert.Equal(-50.0, target.M32);
        }

        [Fact]
        public void CalculateMatrix_StretchMode_Uniform()
        {
            var panelBounds = new Rect(0, 0, 300, 200);
            var elementBounds = new Rect(0, 0, 100, 100);
            var target = ZoomHelper.CalculateMatrix(panelBounds.Width, panelBounds.Height, elementBounds.Width, elementBounds.Height, StretchMode.Uniform);
            Assert.Equal(2.0, target.M11);
            Assert.Equal(0.0, target.M12);
            Assert.Equal(0.0, target.M21);
            Assert.Equal(2.0, target.M22);
            Assert.Equal(-50.0, target.M31);
            Assert.Equal(-50.0, target.M32);
        }

        [Fact]
        public void CalculateMatrix_StretchMode_UniformToFill()
        {
            var panelBounds = new Rect(0, 0, 300, 200);
            var elementBounds = new Rect(0, 0, 100, 100);
            var target = ZoomHelper.CalculateMatrix(panelBounds.Width, panelBounds.Height, elementBounds.Width, elementBounds.Height, StretchMode.UniformToFill);
            Assert.Equal(3.0, target.M11);
            Assert.Equal(0.0, target.M12);
            Assert.Equal(0.0, target.M21);
            Assert.Equal(3.0, target.M22);
            Assert.Equal(-100.0, target.M31);
            Assert.Equal(-100.0, target.M32);
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
        public void CalculateScrollable_ZoomIn_2x_OffsetX_Negative()
        {
            var bounds = new Rect(0, 0, 300, 300);
            var matrix = CreateMatrix(scaleX: 2, scaleY: 2, offsetX: -150);
            ZoomHelper.CalculateScrollable(bounds, matrix, out var extent, out var viewport, out var  offset);
            Assert.Equal(new Size(600, 600), extent);
            Assert.Equal(new Size(300, 300), viewport);
            Assert.Equal(new Vector(150, 0), offset);
        }

        [Fact]
        public void CalculateScrollable_ZoomIn_2x_OffsetX_Positive()
        {
            var bounds = new Rect(0, 0, 300, 300);
            var matrix = CreateMatrix(scaleX: 2, scaleY: 2, offsetX: 150);
            ZoomHelper.CalculateScrollable(bounds, matrix, out var extent, out var viewport, out var  offset);
            Assert.Equal(new Size(600, 600), extent);
            Assert.Equal(new Size(300, 300), viewport);
            Assert.Equal(new Vector(0, 0), offset);
        }

        [Fact]
        public void CalculateScrollable_ZoomIn_2x_OffsetY_Negative()
        {
            var bounds = new Rect(0, 0, 300, 300);
            var matrix = CreateMatrix(scaleX: 2, scaleY: 2, offsetY: -150);
            ZoomHelper.CalculateScrollable(bounds, matrix, out var extent, out var viewport, out var  offset);
            Assert.Equal(new Size(600, 600), extent);
            Assert.Equal(new Size(300, 300), viewport);
            Assert.Equal(new Vector(0, 150), offset);
        }

        [Fact]
        public void CalculateScrollable_ZoomIn_2x_OffsetY_Positive()
        {
            var bounds = new Rect(0, 0, 300, 300);
            var matrix = CreateMatrix(scaleX: 2, scaleY: 2, offsetY: 150);
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

        [Fact]
        public void CalculateScrollable_ZoomOut_0_5x_OffsetX_Negative()
        {
            var bounds = new Rect(0, 0, 300, 300);
            var matrix = CreateMatrix(scaleX: 0.5, scaleY: 0.5, offsetX: -200);
            ZoomHelper.CalculateScrollable(bounds, matrix, out var extent, out var viewport, out var  offset);
            Assert.Equal(new Size(500, 300), extent);
            Assert.Equal(new Size(300, 300), viewport);
            Assert.Equal(new Vector(200, 0), offset);
        }

        [Fact]
        public void CalculateScrollable_ZoomOut_0_5x_OffsetX_Positive()
        {
            var bounds = new Rect(0, 0, 300, 300);
            var matrix = CreateMatrix(scaleX: 0.5, scaleY: 0.5, offsetX: 200);
            ZoomHelper.CalculateScrollable(bounds, matrix, out var extent, out var viewport, out var  offset);
            Assert.Equal(new Size(350, 300), extent);
            Assert.Equal(new Size(300, 300), viewport);
            Assert.Equal(new Vector(0, 0), offset);
        }

        [Fact]
        public void CalculateScrollable_ZoomOut_0_5x_OffsetY_Negative()
        {
            var bounds = new Rect(0, 0, 300, 300);
            var matrix = CreateMatrix(scaleX: 0.5, scaleY: 0.5, offsetY: -200);
            ZoomHelper.CalculateScrollable(bounds, matrix, out var extent, out var viewport, out var  offset);
            Assert.Equal(new Size(300, 500), extent);
            Assert.Equal(new Size(300, 300), viewport);
            Assert.Equal(new Vector(0, 200), offset);
        }

        [Fact]
        public void CalculateScrollable_ZoomOut_0_5x_OffsetY_Positive()
        {
            var bounds = new Rect(0, 0, 300, 300);
            var matrix = CreateMatrix(scaleX: 0.5, scaleY: 0.5, offsetY: 200);
            ZoomHelper.CalculateScrollable(bounds, matrix, out var extent, out var viewport, out var  offset);
            Assert.Equal(new Size(300, 350), extent);
            Assert.Equal(new Size(300, 300), viewport);
            Assert.Equal(new Vector(0, 0), offset);
        }
    }
}
