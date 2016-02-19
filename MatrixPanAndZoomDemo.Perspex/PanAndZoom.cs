using Perspex;
using Perspex.Controls;
using Perspex.Input;
using Perspex.Media;
using System;

namespace MatrixPanAndZoomDemo.Perspex
{
    public enum AutoFitMode { None, Extent, Fill }

    public class PanAndZoom : Border
    {
        private Control _element;
        private Point _pan;
        private Point _previous;
        private Matrix _matrix;

        public double ZoomSpeed { get; set; }

        public AutoFitMode AutoFitMode { get; set; }

        public PanAndZoom()
            : base()
        {
            _matrix = MatrixHelper.Identity;

            ZoomSpeed = 1.2;
            AutoFitMode = AutoFitMode.None;

            Focusable = true;
            Background = Brushes.Transparent;

            DetachedFromVisualTree += PanAndZoom_DetachedFromVisualTree;

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

        private void PanAndZoom_DetachedFromVisualTree(object sender, VisualTreeAttachmentEventArgs e)
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

        public static Point FixInvalidPointPosition(Matrix matrix, Point point)
        {
            return MatrixHelper.TransformPoint(matrix.Invert(), point);
        }

        private void Border_PointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            if (_element != null)
            {
                Point point = e.GetPosition(_element);
                point = FixInvalidPointPosition(_matrix, point);
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
                            point = FixInvalidPointPosition(_matrix, point);
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
                point = FixInvalidPointPosition(_matrix, point);
                PanTo(point);
            }
        }

        private void Element_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.E && _element != null)
            {
                Extent();
            }

            if (e.Key == Key.F && _element != null)
            {
                Fill();
            }

            if (e.Key == Key.R && _element != null)
            {
                Reset();
            }

            if (e.Key == Key.T)
            {
                ToggleAutoFitMode();
                AutoFit();
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_element != null && _element.IsMeasureValid)
            {
                AutoFit(new Rect(0.0, 0.0, finalSize.Width, finalSize.Height), _element.Bounds);
            }

            return base.ArrangeOverride(finalSize);
        }

        public void Invalidate()
        {
            if (_element != null)
            {
                _element.TransformOrigin = new RelativePoint(new Point(0, 0), RelativeUnit.Relative);
                _element.RenderTransform = new MatrixTransform(_matrix);
                _element.InvalidateVisual();
            }
        }

        public void ZoomTo(double zoom, Point point)
        {
            _matrix = MatrixHelper.ScaleAtPrepend(_matrix, zoom, zoom, point.X, point.Y);

            Invalidate();
        }

        public void ZoomDeltaTo(double delta, Point point)
        {
            ZoomTo(delta > 0 ? ZoomSpeed : 1 / ZoomSpeed, point);
        }

        public void StartPan(Point point)
        {
            _pan = new Point();
            _previous = new Point(point.X, point.Y);
        }

        public void PanTo(Point point)
        {
            Point delta = new Point(point.X - _previous.X, point.Y - _previous.Y);
            _previous = new Point(point.X, point.Y);

            _pan = new Point(_pan.X + delta.X, _pan.Y + delta.Y);
            _matrix = MatrixHelper.TranslatePrepend(_matrix, _pan.X, _pan.Y);

            Invalidate();
        }

        public void Extent(Rect panelSize, Rect elementSize)
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

        public void Fill(Rect panelSize, Rect elementSize)
        {
            if (_element != null)
            {
                double pw = panelSize.Width;
                double ph = panelSize.Height;
                double ew = elementSize.Width;
                double eh = elementSize.Height;
                double zx = pw / ew;
                double zy = ph / eh;

                _matrix = MatrixHelper.ScaleAt(zx, zy, ew / 2.0, eh / 2.0);

                Invalidate();
            }
        }

        public void AutoFit(Rect panelSize, Rect elementSize)
        {
            if (_element != null)
            {
                switch (AutoFitMode)
                {
                    case AutoFitMode.None:
                        Reset();
                        break;
                    case AutoFitMode.Extent:
                        Extent(panelSize, elementSize);
                        break;
                    case AutoFitMode.Fill:
                        Fill(panelSize, elementSize);
                        break;
                }

                Invalidate();
            }
        }

        public void ToggleAutoFitMode()
        {
            switch (AutoFitMode)
            {
                case AutoFitMode.None:
                    AutoFitMode = AutoFitMode.Extent;
                    break;
                case AutoFitMode.Extent:
                    AutoFitMode = AutoFitMode.Fill;
                    break;
                case AutoFitMode.Fill:
                    AutoFitMode = AutoFitMode.None;
                    break;
            }
        }

        public void Reset()
        {
            _matrix = MatrixHelper.Identity;

            Invalidate();
        }

        private void Extent()
        {
            Extent(this.Bounds, _element.Bounds);
        }

        private void Fill()
        {
            Fill(this.Bounds, _element.Bounds);
        }

        public void AutoFit()
        {
            if (_element != null)
            {
                AutoFit(this.Bounds, _element.Bounds);
            }
        }
    }
}
