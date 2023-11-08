using Content.Shared.Power.Substation;

namespace Content.Client.Power.Substation;

[RegisterComponent]
public sealed partial class SubstationVisualsComponent : Component
{
    [DataField("screenLayer")]
    public string LayerMap { get; private set; } = string.Empty;

    [DataField("integrityStates")]
    public Dictionary<SubstationIntegrityState, string> IntegrityStates = new();
}
