// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace UnoDemo.ViewModels;

public class RotationViewModel : ViewModelBase
{
    private double _currentRotation;

    public double CurrentRotation
    {
        get => _currentRotation;
        set => SetProperty(ref _currentRotation, value);
    }

    public ZoomBorder? ZoomBorder { get; set; }

    public void UpdateRotation()
    {
        if (ZoomBorder == null) return;
        CurrentRotation = ZoomBorder.Rotation;
    }

    public void RotateBy(double degrees)
    {
        ZoomBorder?.Rotate(degrees);
        UpdateRotation();
    }

    public void Reset()
    {
        ZoomBorder?.ResetRotation();
        UpdateRotation();
    }

    public void Snap()
    {
        ZoomBorder?.SnapRotation();
        UpdateRotation();
    }
}
