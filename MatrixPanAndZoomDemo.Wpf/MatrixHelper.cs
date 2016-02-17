using System;
using System.Windows.Media;

namespace MatrixPanAndZoomDemo.Wpf
{
    public static class MatrixHelper
    {
        public static Matrix Translation(double offsetX, double offsetY)
        {
            return new Matrix(1.0, 0.0, 0.0, 1.0, offsetX, offsetY);
        }
        
        public static Matrix TranslatePrepend(Matrix matrix, double offsetX, double offsetY)
        {
            return Translation(offsetX, offsetY) * matrix;
        }
    }
}
