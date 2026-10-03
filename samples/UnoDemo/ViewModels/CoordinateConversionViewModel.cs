// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace UnoDemo.ViewModels;

public class CoordinateConversionViewModel : ViewModelBase
{
    private double _sourceX = 150;
    private double _sourceY = 150;
    private string _elementPoint = "";
    private string _sourcePoint = "";

    public double SourceX
    {
        get => _sourceX;
        set
        {
            if (SetProperty(ref _sourceX, value))
            {
                UpdateConversions();
            }
        }
    }

    public double SourceY
    {
        get => _sourceY;
        set
        {
            if (SetProperty(ref _sourceY, value))
            {
                UpdateConversions();
            }
        }
    }

    public string ElementPoint
    {
        get => _elementPoint;
        set => SetProperty(ref _elementPoint, value);
    }

    public string SourcePoint
    {
        get => _sourcePoint;
        set => SetProperty(ref _sourcePoint, value);
    }

    public ZoomBorder? ZoomBorder { get; set; }

    public void UpdateConversions()
    {
        if (ZoomBorder == null) return;

        var sourcePoint = new Point(SourceX, SourceY);
        var elementPoint = ZoomBorder.ContentToViewport(sourcePoint);
        ElementPoint = $"Viewport: ({elementPoint.X:F2}, {elementPoint.Y:F2})";

        var backToSource = ZoomBorder.ViewportToContent(elementPoint);
        SourcePoint = $"Back to Content: ({backToSource.X:F2}, {backToSource.Y:F2})";
    }
}
