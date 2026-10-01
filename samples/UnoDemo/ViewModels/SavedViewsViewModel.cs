// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System.Collections.ObjectModel;

namespace UnoDemo.ViewModels;

public class SavedViewsViewModel : ViewModelBase
{
    private string _viewName = "";
    private string _viewDescription = "";
    private string? _selectedViewName;

    public string ViewName
    {
        get => _viewName;
        set => SetProperty(ref _viewName, value);
    }

    public string ViewDescription
    {
        get => _viewDescription;
        set => SetProperty(ref _viewDescription, value);
    }

    public string? SelectedViewName
    {
        get => _selectedViewName;
        set => SetProperty(ref _selectedViewName, value);
    }

    public ObservableCollection<string> SavedViews { get; } = new();

    public ZoomBorder? ZoomBorder { get; set; }

    public void RefreshViewList()
    {
        if (ZoomBorder == null) return;

        SavedViews.Clear();
        var names = ZoomBorder.GetSavedViewNames();
        foreach (var name in names)
        {
            SavedViews.Add(name);
        }
    }

    public void SaveView()
    {
        if (ZoomBorder == null || string.IsNullOrWhiteSpace(ViewName)) return;

        ZoomBorder.SaveView(ViewName, ViewDescription);
        RefreshViewList();
        ViewName = "";
        ViewDescription = "";
    }

    public void RestoreView()
    {
        if (ZoomBorder == null || string.IsNullOrWhiteSpace(SelectedViewName)) return;

        ZoomBorder.RestoreView(SelectedViewName);
    }

    public void DeleteView()
    {
        if (ZoomBorder == null || string.IsNullOrWhiteSpace(SelectedViewName)) return;

        ZoomBorder.DeleteSavedView(SelectedViewName);
        RefreshViewList();
        SelectedViewName = null;
    }

    public void ClearAllViews()
    {
        if (ZoomBorder == null) return;

        ZoomBorder.ClearSavedViews();
        RefreshViewList();
        SelectedViewName = null;
    }
}
