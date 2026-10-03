// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace UnoDemo.ViewModels;

public class AccessibilityViewModel : ViewModelBase
{
    private string _zoomLevelDescription = "";
    private string _panPositionDescription = "";
    private string _fullDescription = "";

    public string ZoomLevelDescription
    {
        get => _zoomLevelDescription;
        set => SetProperty(ref _zoomLevelDescription, value);
    }

    public string PanPositionDescription
    {
        get => _panPositionDescription;
        set => SetProperty(ref _panPositionDescription, value);
    }

    public string FullDescription
    {
        get => _fullDescription;
        set => SetProperty(ref _fullDescription, value);
    }

    public ZoomBorder? ZoomBorder { get; set; }

    public void UpdateDescriptions()
    {
        if (ZoomBorder == null) return;

        ZoomBorder.UpdateAccessibilityDescriptions();

        ZoomLevelDescription = ZoomBorder.ZoomLevelDescription;
        PanPositionDescription = ZoomBorder.PanPositionDescription;
        FullDescription = ZoomBorder.GetAccessibilityDescription();
    }
}
