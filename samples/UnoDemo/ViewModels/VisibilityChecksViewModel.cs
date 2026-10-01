// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace UnoDemo.ViewModels;

public class VisibilityChecksViewModel : ViewModelBase
{
    private double _pointX = 250;
    private double _pointY = 200;
    private double _rectX = 100;
    private double _rectY = 100;
    private double _rectWidth = 200;
    private double _rectHeight = 150;
    private string _pointResult = "";
    private string _rectResult = "";
    private string _portionResult = "";

    public double PointX
    {
        get => _pointX;
        set => SetProperty(ref _pointX, value);
    }

    public double PointY
    {
        get => _pointY;
        set => SetProperty(ref _pointY, value);
    }

    public double RectX
    {
        get => _rectX;
        set => SetProperty(ref _rectX, value);
    }

    public double RectY
    {
        get => _rectY;
        set => SetProperty(ref _rectY, value);
    }

    public double RectWidth
    {
        get => _rectWidth;
        set => SetProperty(ref _rectWidth, value);
    }

    public double RectHeight
    {
        get => _rectHeight;
        set => SetProperty(ref _rectHeight, value);
    }

    public string PointResult
    {
        get => _pointResult;
        set => SetProperty(ref _pointResult, value);
    }

    public string RectResult
    {
        get => _rectResult;
        set => SetProperty(ref _rectResult, value);
    }

    public string PortionResult
    {
        get => _portionResult;
        set => SetProperty(ref _portionResult, value);
    }

    public ZoomBorder? ZoomBorder { get; set; }

    public void CheckPointVisibility()
    {
        if (ZoomBorder == null) return;

        var point = new Point(PointX, PointY);
        var isVisible = ZoomBorder.IsPointVisible(point);
        PointResult = $"Point ({PointX:F0}, {PointY:F0}): {(isVisible ? "VISIBLE ✓" : "NOT VISIBLE ✗")}";
    }

    public void CheckRectVisibility()
    {
        if (ZoomBorder == null) return;

        var rect = new Rect(RectX, RectY, RectWidth, RectHeight);
        var isVisible = ZoomBorder.IsRectangleVisible(rect);
        RectResult = $"Rectangle: {(isVisible ? "VISIBLE ✓" : "NOT VISIBLE ✗")}";
    }

    public void GetVisiblePortion()
    {
        if (ZoomBorder == null) return;

        var rect = new Rect(RectX, RectY, RectWidth, RectHeight);
        var visiblePortion = ZoomBorder.GetVisiblePortion(rect);

        if (visiblePortion.Width == 0 && visiblePortion.Height == 0)
        {
            PortionResult = "Visible Portion: NONE";
        }
        else
        {
            PortionResult = $"Visible Portion: X={visiblePortion.X:F0}, Y={visiblePortion.Y:F0}, W={visiblePortion.Width:F0}, H={visiblePortion.Height:F0}";
        }
    }
}
