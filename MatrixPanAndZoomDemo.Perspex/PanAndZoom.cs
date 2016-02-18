using Perspex;
using Perspex.Controls;
using Perspex.Input;
using Perspex.Media;
using System;

namespace MatrixPanAndZoomDemo.Perspex
{
    public class PanAndZoom : Border
    {
        private Control _element;
        private double _zoomSpeed = 1.2;
        //private Point _pan;
        private Point _previous;
        private Matrix _matrix = MatrixHelper.Identity;

        public PanAndZoom()
            : base()
        {
            Focusable = true;
            Background = Brushes.Transparent;
            DetachedFromVisualTree += UIElementZoomManager_DetachedFromVisualTree;

            this.GetObservable(ChildProperty).Subscribe(value =>
            {
                if (value != null && value != _element && _element != null)
                {
                    Unload();
                }

                if (value != null && value != _element)
                {
                    Initialize(value);
                }
            });
        }

        private void UIElementZoomManager_DetachedFromVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            if (_element != null)
            {
                Unload();
            }
        }

        private void Initialize(Control element)
        {
            if (element != null)
            {
                _element = element;
                this.Focus();
                this.PointerWheelChanged += Border_PointerWheelChanged;
                this.PointerPressed += Border_PointerPressed;
                this.PointerReleased += Border_PointerReleased;
                this.PointerMoved += Border_PointerMoved;
                this.KeyDown += Element_KeyDown;
            }
        }

        private void Unload()
        {
            if (_element != null)
            {
                this.PointerWheelChanged -= Border_PointerWheelChanged;
                this.PointerPressed -= Border_PointerPressed;
                this.PointerReleased -= Border_PointerReleased;
                this.PointerMoved -= Border_PointerMoved;
                this.KeyDown -= Element_KeyDown;
                _element.RenderTransform = null;
                _element = null;
            }
        }

        private void Invalidate()
        {
            if (_element != null)
            {
                _element.TransformOrigin = new RelativePoint(new Point(0, 0), RelativeUnit.Relative);
                _element.RenderTransform = new MatrixTransform(_matrix);
                _element.InvalidateVisual();
            }
        }

        private void ZoomTo(double zoom, Point point)
        {
            _matrix = MatrixHelper.ScaleAtPrepend(_matrix, zoom, zoom, point.X, point.Y);

            Invalidate();
        }

        private void ZoomDeltaTo(double delta, Point point)
        {
            ZoomTo(delta > 0 ? _zoomSpeed : 1 / _zoomSpeed, point);
        }

        private void StartPan(Point point)
        {
            //_pan = new Point();
            _previous = new Point(point.X, point.Y);
        }

        private void PanTo(Point point)
        {
            Point delta = new Point(point.X - _previous.X, point.Y - _previous.Y);
            _previous = new Point(point.X, point.Y);

            //_pan = new Point(_pan.X + delta.X, _pan.Y + delta.Y);
            //_matrix = MatrixHelper.TranslatePrepend(_matrix, _pan.X, _pan.Y);
            _matrix = MatrixHelper.TranslatePrepend(_matrix, delta.X, delta.Y);

            Invalidate();
        }

        private void Extent()
        {
            if (_element != null)
            {
                double pw = this.Bounds.Width;
                double ph = this.Bounds.Height;
                double ew = _element.Bounds.Width;
                double eh = _element.Bounds.Height;
                double zx = pw / ew;
                double zy = ph / eh;
                double zoom = Math.Min(zx, zy);

                _matrix = MatrixHelper.ScaleAt(zoom, zoom, ew / 2.0, eh / 2.0);

                Invalidate();
            }
        }

        private void Fill()
        {
            if (_element != null)
            {
                double pw = this.Bounds.Width;
                double ph = this.Bounds.Height;
                double ew = _element.Bounds.Width;
                double eh = _element.Bounds.Height;
                double zx = pw / ew;
                double zy = ph / eh;

                _matrix = MatrixHelper.ScaleAt(zx, zy, ew / 2.0, eh / 2.0);

                Invalidate();
            }
        }

        private void Reset()
        {
            _matrix = MatrixHelper.Identity;

            Invalidate();
        }

        private void Border_PointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            if (_element != null)
            {
                Point point = e.GetPosition(_element);
                ZoomDeltaTo(e.Delta.Y, point);
            }
        }

        private void Border_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            switch (e.MouseButton)
            {
                case MouseButton.Right:
                    {
                        if (_element != null)
                        {
                            Point point = e.GetPosition(_element);
                            StartPan(point);
                            e.Device.Capture(_element);
                        }
                    }
                    break;
            }
        }

        private void Border_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (_element != null)
            {
                switch (e.MouseButton)
                {
                    case MouseButton.Right:
                        {
                            if (_element != null && e.Device.Captured == _element)
                            {
                                e.Device.Capture(null);
                            }
                        }
                        break;
                }
            }
        }

        private void Border_PointerMoved(object sender, PointerEventArgs e)
        {
            if (_element != null && e.Device.Captured == _element)
            {
                Point point = e.GetPosition(_element);
                PanTo(point);
            }
        }

        private void Element_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.E)
            {
                Extent();
            }

            if (e.Key == Key.F)
            {
                Fill();
            }

            if (e.Key == Key.R)
            {
                Reset();
            }
        }
    }
}
