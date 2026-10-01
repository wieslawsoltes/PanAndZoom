// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace UnoDemo.ViewModels;

public class BoundsCallbacksViewModel : ViewModelBase
{
    private string _visibleArea = "";
    private string _contentBounds = "";
    private string _eventLog = "";

    public string VisibleArea
    {
        get => _visibleArea;
        set => SetProperty(ref _visibleArea, value);
    }

    public string ContentBounds
    {
        get => _contentBounds;
        set => SetProperty(ref _contentBounds, value);
    }

    public string EventLog
    {
        get => _eventLog;
        set => SetProperty(ref _eventLog, value);
    }

    public ZoomBorder? ZoomBorder { get; set; }

    public void UpdateBounds()
    {
        if (ZoomBorder == null) return;

        var visibleArea = ZoomBorder.GetVisibleContentBounds();
        VisibleArea = $"Visible Content: X={visibleArea.X:F2}, Y={visibleArea.Y:F2}, W={visibleArea.Width:F2}, H={visibleArea.Height:F2}";

        var viewportBounds = ZoomBorder.GetViewportBounds();
        ContentBounds = $"Viewport: X={viewportBounds.X:F2}, Y={viewportBounds.Y:F2}, W={viewportBounds.Width:F2}, H={viewportBounds.Height:F2}";
    }

    public void LogEvent(string message)
    {
        EventLog = $"{message}\n{EventLog}";
        if (EventLog.Length > 1000)
        {
            EventLog = EventLog.Substring(0, 1000);
        }
    }
}
