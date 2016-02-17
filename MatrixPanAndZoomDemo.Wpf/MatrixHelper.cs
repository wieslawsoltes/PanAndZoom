using System;
using System.Windows.Media;

namespace MatrixPanAndZoomDemo.Wpf
{
    public static class MatrixHelper
    {
        public static Matrix Translation(double offsetX, double offsetY)
        {
            var result = Matrix.Identity;
            result.OffsetX = offsetX;
            result.OffsetY = offsetY;
            return result;
        }
        
        public static Matrix TranslatePrepend(Matrix matrix, double offsetX, double offsetY)
        {
            return Translation(offsetX, offsetY) * matrix;
        }
    }
}
