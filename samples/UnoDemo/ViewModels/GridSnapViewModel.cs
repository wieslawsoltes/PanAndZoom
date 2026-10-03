// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace UnoDemo.ViewModels;

public class GridSnapViewModel : ViewModelBase
{
    private double _testValue = 123.45;
    private double _testX = 123.45;
    private double _testY = 678.90;
    private string _snappedValue = "";
    private string _snappedPoint = "";

    public double TestValue
    {
        get => _testValue;
        set
        {
            if (SetProperty(ref _testValue, value))
            {
                UpdateSnapping();
            }
        }
    }

    public double TestX
    {
        get => _testX;
        set
        {
            if (SetProperty(ref _testX, value))
            {
                UpdateSnapping();
            }
        }
    }

    public double TestY
    {
        get => _testY;
        set
        {
            if (SetProperty(ref _testY, value))
            {
                UpdateSnapping();
            }
        }
    }

    public string SnappedValue
    {
        get => _snappedValue;
        set => SetProperty(ref _snappedValue, value);
    }

    public string SnappedPoint
    {
        get => _snappedPoint;
        set => SetProperty(ref _snappedPoint, value);
    }

    public ZoomBorder? ZoomBorder { get; set; }

    public void UpdateSnapping()
    {
        if (ZoomBorder == null) return;

        var snappedValue = ZoomBorder.SnapToGrid(TestValue);
        SnappedValue = $"Snapped Value: {snappedValue:F2}";

        var snappedPoint = ZoomBorder.SnapToGrid(new Point(TestX, TestY));
        SnappedPoint = $"Snapped Point: ({snappedPoint.X:F2}, {snappedPoint.Y:F2})";
    }
}
