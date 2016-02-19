using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MatrixPanAndZoomDemo.Wpf
{
    public enum AutoFitMode { None, Reset, Extent, Fill }

    public class PanAndZoom : Border
    {
        private UIElement _element;
        private double _zoomSpeed = 1.2;
        private AutoFitMode _autoFitModde = AutoFitMode.None;
        private Point _pan;
        private Point _previous;
        private Matrix _matrix = MatrixHelper.Identity;

        public PanAndZoom()
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
                this.PreviewMouseWheel += Border_PreviewMouseWheel;
                this.PreviewMouseRightButtonDown += Border_PreviewMouseRightButtonDown;
                this.PreviewMouseRightButtonUp += Border_PreviewMouseRightButtonUp;
                this.PreviewMouseMove += Border_PreviewMouseMove;
                this.KeyDown += Border_KeyDown;
            }
        }

        private void Unload()
        {
            if (_element != null)
            {
                this.PreviewMouseWheel -= Border_PreviewMouseWheel;
                this.PreviewMouseRightButtonDown -= Border_PreviewMouseRightButtonDown;
                this.PreviewMouseRightButtonUp -= Border_PreviewMouseRightButtonUp;
                this.PreviewMouseMove -= Border_PreviewMouseMove;
                this.KeyDown -= Border_KeyDown;
                _element.RenderTransform = null;
                _element = null;
            }
        }

        private void Invalidate()
        {
            if (_element != null)
            {
                _element.RenderTransformOrigin = new Point(0, 0);
                _element.RenderTransform = new MatrixTransform(_matrix);
                _element.InvalidateVisual();
            }
        }

        private void ZoomTo(double zoom, Point point)
        {
            _matrix = MatrixHelper.ScaleAtPrepend(_matrix, zoom, zoom, point.X, point.Y);

            Invalidate();
        }

        private void ZoomDeltaTo(int delta, Point point)
        {
            ZoomTo(delta > 0 ? _zoomSpeed : 1 / _zoomSpeed, point);
        }

        private void StartPan(Point point)
        {
            _pan = new Point();
            _previous = new Point(point.X, point.Y);
        }

        private void PanTo(Point point)
        {
            Point delta = new Point(point.X - _previous.X, point.Y - _previous.Y);
            _previous = new Point(point.X, point.Y);

            _pan = new Point(_pan.X + delta.X, _pan.Y + delta.Y);
            _matrix = MatrixHelper.TranslatePrepend(_matrix, _pan.X, _pan.Y);

            Invalidate();
        }

        private void Extent(Size panelSize, Size elementSize)
        {
            if (_element != null)
            {
                double pw = panelSize.Width;
                double ph = panelSize.Height;
                double ew = elementSize.Width;
                double eh = elementSize.Height;
                double zx = pw / ew;
                double zy = ph / eh;
                double zoom = Math.Min(zx, zy);

                _matrix = MatrixHelper.ScaleAt(zoom, zoom, ew > pw ? 0.0 : ew / 2.0, eh > ph ? 0.0 : eh / 2.0);

                Invalidate();
            }
        }

        private void Fill(Size panelSize, Size elementSize)
        {
            if (_element != null)
            {
                double pw = panelSize.Width;
                double ph = panelSize.Height;
                double ew = elementSize.Width;
                double eh = elementSize.Height;
                double zx = pw / ew;
                double zy = ph / eh;

                _matrix = MatrixHelper.ScaleAt(zx, zy, ew > pw ? 0.0 : ew / 2.0, eh > ph ? 0.0 : eh / 2.0);

                Invalidate();
            }
        }

        private void Reset()
        {
            _matrix = MatrixHelper.Identity;

            Invalidate();
        }

        private void Border_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_element != null)
            {
                Point point = e.GetPosition(_element);
                ZoomDeltaTo(e.Delta, point);
            }
        }

        private void Border_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_element != null)
            {
                Point point = e.GetPosition(_element);
                StartPan(point);
                _element.CaptureMouse();
            }
        }

        private void Border_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_element != null)
            {
                _element.ReleaseMouseCapture();
            }
        }

        private void Border_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_element != null && _element.IsMouseCaptured)
            {
                Point point = e.GetPosition(_element);
                PanTo(point);
            }
        }

        private void Border_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.E && _element != null)
            {
                Extent(this.RenderSize, _element.RenderSize);
            }

            if (e.Key == Key.F && _element != null)
            {
                Fill(this.RenderSize, _element.RenderSize);
            }

            if (e.Key == Key.R && _element != null)
            {
                Reset();
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_element != null)
            {
                switch (_autoFitModde)
                {
                    case AutoFitMode.Reset:
                        Reset();
                        break;
                    case AutoFitMode.Extent:
                        Extent(this.RenderSize, _element.RenderSize);
                        break;
                    case AutoFitMode.Fill:
                        Fill(this.RenderSize, _element.RenderSize);
                        break;
                }
            }

            return base.ArrangeOverride(finalSize);
        }
    }
}
