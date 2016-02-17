using Perspex;

namespace MatrixPanAndZoomDemo.Perspex
{
    // http://referencesource.microsoft.com/#WindowsBase/Base/System/Windows/Media/Matrix.cs,13b7d422c46ba7a0,references
    // https://github.com/sharpdx/SharpDX/blob/master/Source/SharpDX.Mathematics/Matrix3x2.cs

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

        public static Matrix ScaleAt(double scaleX, double scaleY, double centerX, double centerY)
        {
            //Point center = new Point(centerX, centerY);
            //return Matrix.CreateTranslation(-center)
            //    * Matrix.CreateScale(scaleX, scaleY)
            //    * Matrix.CreateTranslation(center);
            return new Matrix(scaleX, 0, 0, scaleY, centerX - scaleX * centerX, centerY - scaleY * centerY);
        }

        public static Matrix ScaleAtPrepend(Matrix matrix, double scaleX, double scaleY, double centerX, double centerY)
        {
            //Point center = new Point(centerX, centerY);
            //return Matrix.CreateTranslation(-center)
            //    * Matrix.CreateScale(scaleX, scaleY)
            //    * Matrix.CreateTranslation(center)
            //    * matrix;
            return new Matrix(scaleX, 0, 0, scaleY, centerX - scaleX * centerX, centerY - scaleY * centerY) * matrix;
        }


        public static Matrix Rotation(double angle, Vector center)
        {
            return
                Matrix.CreateTranslation(-center)
                * Matrix.CreateRotation(angle)
                * Matrix.CreateTranslation(center);
        }

        public static Point TransformPoint(Matrix matrix, Point point)
        {
            return new Point(
                (point.X * matrix.M11) + (point.Y * matrix.M21) + matrix.M31,
                (point.X * matrix.M12) + (point.Y * matrix.M22) + matrix.M32);
        }
    }
}
