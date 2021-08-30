using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using Avalonia.Animation;
using Avalonia.Media;
using Avalonia.Media.Transformation;

namespace Avalonia.Controls.PanAndZoom
{
    /// <summary>
    /// Defines how a TransformOperations property should be animated.
    /// 
    /// Workaround for https://github.com/AvaloniaUI/Avalonia/issues/6494
    /// </summary>
    public class TransformOperationsTransition : Transition<ITransform>
    {
        /// <inheritdoc/>
        public override IObservable<ITransform> DoTransition(IObservable<double> progress, ITransform oldValue, ITransform newValue)
        {
            return progress.Select(p =>
           {
               double f = Easing.Ease(p);

               TransformOperations.Builder builder = new TransformOperations.Builder(1);

               Matrix matrix1 = (oldValue as TransformOperations)?.Value ?? Matrix.Identity;
               Matrix matrix2 = (newValue as TransformOperations)?.Value ?? Matrix.Identity;

               Matrix result = new Matrix(matrix1.M11 + (matrix2.M11 - matrix1.M11) * f,
                   matrix1.M12 + (matrix2.M12 - matrix1.M12) * f,
                   matrix1.M21 + (matrix2.M21 - matrix1.M21) * f,
                   matrix1.M22 + (matrix2.M22 - matrix1.M22) * f,
                   matrix1.M31 + (matrix2.M31 - matrix1.M31) * f,
                   matrix1.M32 + (matrix2.M32 - matrix1.M32) * f);

               builder.AppendMatrix(result);

               return builder.Build();
           });
        }
    }
}
