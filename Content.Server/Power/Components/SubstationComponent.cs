using Content.Shared.Power.Substation;

namespace Content.Server.Power.Components;

[RegisterComponent]
public sealed partial class SubstationComponent : Component
{

    [ViewVariables(VVAccess.ReadOnly)]
    public float LastIntegrity = 100f;

    [DataField("decayEnabled")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool DecayEnabled = true;
   
    [DataField("state")]
    [ViewVariables(VVAccess.ReadWrite)]
    public SubstationIntegrityState State = SubstationIntegrityState.Healthy;

    //9.231205828 is the amount of moles in a 5L container (the default fuse) at 1000Kpa 20C°
    public float initialFuseMoles = 2.051379050f; 

}
