using Robust.Shared.Serialization;

namespace Content.Client.Atmos.Visualizers;

[RegisterComponent]
public sealed class GasThermoMachineVisualsComponent : Component
{
    [DataField("offState", required: true)]
    public string OffState = default!;

    [DataField("onState", required: true)]
    public string OnState = default!;

    [DataField("panelOpen", required: true)]
    public string PanelOpen = default!;

    [DataField("panelClose", required: true)]
    public string PanelClose = default!;

    [DataField("panelOn", required: true)]
    public string PanelOn = default!;

}

