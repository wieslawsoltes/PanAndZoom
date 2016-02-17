using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MatrixPanAndZoomDemo.Wpf
{
    public class UIElementZoomManager : Border
    {
        private UIElement _element;
        private double _zoomSpeed = 1.2;
        private Point _pan;
        private Point _previous;
        private Matrix _matrix = Matrix.Identity;

        public UIElementZoomManager()
            : base()
        {
            Focusable = true;
            Background = Brushes.Transparent;
            Unloaded += UIElementZoomManager_Unloaded;
        }

        private void UIElementZoomManager_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_element != null)
            {
                Unload();
            }
        }

        public override UIElement Child
        {
            get { return base.Child; }
            set
            {
                if (value != null && value != _element && _element != null)
                {
                    Unload();
                }

                base.Child = value;

                if (value != null && value != _element)
                {
                    Initialize(value);
                }
            }
        }

        private void Initialize(UIElement element)
        {
            if (element != null)
            {
                _element = element;
                this.Focus();
                this.PreviewMouseWheel += Element_PreviewMouseWheel;
                this.PreviewMouseRightButtonDown += Element_PreviewMouseRightButtonDown;
                this.PreviewMouseRightButtonUp += Element_PreviewMouseRightButtonUp;
                this.PreviewMouseMove += Element_PreviewMouseMove;
                this.KeyDown += Element_KeyDown;
            }
        }

        private void Unload()
        {
            if (_element != null)
            {
                this.PreviewMouseWheel -= Element_PreviewMouseWheel;
                this.PreviewMouseRightButtonDown -= Element_PreviewMouseRightButtonDown;
                this.PreviewMouseRightButtonUp -= Element_PreviewMouseRightButtonUp;
                this.PreviewMouseMove -= Element_PreviewMouseMove;
                this.KeyDown -= Element_KeyDown;
                _element.RenderTransform = null;
                _element = null;
            }
        }

        private void Invalidate()
        {
            if (_element != null)
            {
                _element.RenderTransform = new MatrixTransform(_matrix);
                _element.InvalidateVisual();
            }
        }

        private void ZoomAsTo(double zoom, Point point)
        {
            _matrix.ScaleAtPrepend(zoom, zoom, point.X, point.Y);

            Invalidate();
        }

        private void ZoomDeltaTo(int delta, Point point)
        {
            ZoomAsTo(delta > 0 ? _zoomSpeed : 1 / _zoomSpeed, point);
        }

        private void StartPan(Point point)
        {
            _pan = new Point();
            _previous = point;
        }

        private void PanTo(Point point)
        {
            //Vector delta = point - _previous;
            Point delta = new Point(point.X - _previous.X, point.Y - _previous.Y);
            //System.Diagnostics.Debug.Print("delta " + delta);
            _previous = new Point(point.X, point.Y);
            //System.Diagnostics.Debug.Print("pan1 " + _pan);
            //_pan = _pan + delta;
            _pan = new Point(_pan.X + delta.X, _pan.Y + delta.Y);
            System.Diagnostics.Debug.Print("pan2 " + _pan);
            //_matrix.TranslatePrepend(_pan.X, _pan.Y);
            _matrix = MatrixHelper.TranslatePrepend(_matrix, _pan.X, _pan.Y);
 
            Invalidate();
        }

        private void Fit()
        {
            if (_element != null)
            {
                double pw = this.RenderSize.Width;
                double ph = this.RenderSize.Height;
                double ew = _element.RenderSize.Width;
                double eh = _element.RenderSize.Height;
                double zx = pw / ew;
                double zy = ph / eh;
                double zoom = Math.Min(zx, zy);

                _matrix = Matrix.Identity;
                _matrix.ScaleAt(zoom, zoom, ew / 2.0, eh / 2.0);

                Invalidate();
            }
        }

        private void Fill()
        {
            if (_element != null)
            {
                double pw = this.RenderSize.Width;
                double ph = this.RenderSize.Height;
                double ew = _element.RenderSize.Width;
                double eh = _element.RenderSize.Height;
                double zx = pw / ew;
                double zy = ph / eh;

                _matrix = Matrix.Identity;
                _matrix.ScaleAt(zx, zy, ew / 2.0, eh / 2.0);

                Invalidate();
            }
        }

        private void Reset()
        {
            _matrix = Matrix.Identity;

            Invalidate();
        }

        private void Element_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_element != null)
            {
                Point point = e.GetPosition(_element);
                ZoomDeltaTo(e.Delta, point);
            }
        }

        private void Element_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_element != null)
            {
                Point point = e.GetPosition(_element);
                StartPan(point);
                _element.CaptureMouse();
            }
        }

        private void Element_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_element != null)
            {
                _element.ReleaseMouseCapture();
            }
        }

        private void Element_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_element != null && _element.IsMouseCaptured)
            {
                Point point = e.GetPosition(_element);
                PanTo(point);
            }
        }

        private void Element_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Z)
            {
                Reset();
            }

            if (e.Key == Key.X)
            {
                Fit();
            }
        }
    }
}
