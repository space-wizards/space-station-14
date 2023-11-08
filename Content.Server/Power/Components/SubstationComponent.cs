using Content.Shared.Power.Substation;

namespace Content.Server.Power.Components;

[RegisterComponent]
public sealed partial class SubstationComponent : Component
{

    [DataField("integrity")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Integrity = 100.0f;

    [DataField("decayEnabled")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool DecayEnabled = true;
   
    [ViewVariables(VVAccess.ReadWrite)]
    public SubstationIntegrityState State = SubstationIntegrityState.Healthy;

}
