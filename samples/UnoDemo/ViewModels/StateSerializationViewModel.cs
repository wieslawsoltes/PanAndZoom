// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnoDemo.ViewModels;

public class StateSerializationViewModel : ViewModelBase
{
    private string _stateJson = "";
    private ZoomBorderState? _savedState;

    public string StateJson
    {
        get => _stateJson;
        set => SetProperty(ref _stateJson, value);
    }

    public ZoomBorder? ZoomBorder { get; set; }

    public void ExportState()
    {
        if (ZoomBorder == null) return;

        _savedState = ZoomBorder.ExportState();

        // Source generated serialization keeps the demo working on trimmed targets (WebAssembly, iOS, Android).
        StateJson = JsonSerializer.Serialize(_savedState, StateSerializationJsonContext.Default.ZoomBorderState);
    }

    public void ImportState()
    {
        if (ZoomBorder == null || _savedState == null) return;

        ZoomBorder.ImportState(_savedState);
    }

    public void ResetState()
    {
        StateJson = "";
        _savedState = null;
    }
}

[JsonSourceGenerationOptions(
    WriteIndented = true,
    NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals)]
[JsonSerializable(typeof(ZoomBorderState))]
internal partial class StateSerializationJsonContext : JsonSerializerContext
{
}
