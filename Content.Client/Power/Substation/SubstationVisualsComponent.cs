using Content.Shared.Power;

namespace Content.Client.Power.Substation;

[RegisterComponent]
public sealed partial class SubstationVisualsComponent : Component
{
    [DataField]
    public string LayerMap { get; private set; } = string.Empty;

    [DataField]
    public Dictionary<SubstationIntegrityState, string> IntegrityStates = new();
}
