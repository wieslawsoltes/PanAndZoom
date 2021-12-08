using Xunit;

namespace Avalonia.Controls.PanAndZoom.UnitTests;

public class MatrixHelperTests
{
    [Fact]
    public void Translate_Returns_Matrix()
    {
        var target = MatrixHelper.Translate(20, 30);
        Assert.Equal(1.0, target.M11);
        Assert.Equal(0.0, target.M12);
        Assert.Equal(0.0, target.M21);
        Assert.Equal(1.0, target.M22);
        Assert.Equal(20.0, target.M31);
        Assert.Equal(30.0, target.M32);
    }
 
    [Fact]
    public void Scale_Returns_Matrix()
    {
        var target = MatrixHelper.Scale(2, 3);
        Assert.Equal(2.0, target.M11);
        Assert.Equal(0.0, target.M12);
        Assert.Equal(0.0, target.M21);
        Assert.Equal(3.0, target.M22);
        Assert.Equal(0.0, target.M31);
        Assert.Equal(0.0, target.M32);
    }
}