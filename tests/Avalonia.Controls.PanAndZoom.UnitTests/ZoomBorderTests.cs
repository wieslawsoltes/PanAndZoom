using Xunit;

namespace Avalonia.Controls.PanAndZoom.UnitTests
{
    public class ZoomBorderTests
    {
        [Fact]
        public void ZoomBorder_Ctor()
        {
            var target = new ZoomBorder();
            Assert.NotNull(target);
            Assert.Equal(ButtonName.Middle, target.PanButton);
            Assert.Equal(1.2, target.ZoomSpeed);
            Assert.Equal(StretchMode.Uniform, target.Stretch);
            Assert.Equal(1.0, target.ZoomX);
            Assert.Equal(1.0, target.ZoomY);
            Assert.Equal(0.0, target.OffsetX);
            Assert.Equal(0.0, target.OffsetY);
            Assert.True(target.EnableConstrains);
            Assert.Equal(double.NegativeInfinity, target.MinZoomX);
            Assert.Equal(double.PositiveInfinity, target.MaxZoomX);
            Assert.Equal(double.NegativeInfinity, target.MinZoomY);
            Assert.Equal(double.PositiveInfinity, target.MaxZoomY);
            Assert.Equal(double.NegativeInfinity, target.MinOffsetX);
            Assert.Equal(double.PositiveInfinity, target.MaxOffsetX);
            Assert.Equal(double.NegativeInfinity, target.MinOffsetY);
            Assert.Equal(double.PositiveInfinity, target.MaxOffsetY);
            Assert.True(target.EnablePan);
            Assert.True(target.EnableZoom);
            Assert.True(target.EnableGestureZoom); 
            Assert.True(target.EnableGestureRotation);
            Assert.True(target.EnableGestureTranslation);
        }
    }
}
