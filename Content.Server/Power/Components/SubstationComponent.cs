using Content.Shared.Power.Substation;

namespace Content.Server.Power.Components;

[RegisterComponent]
public sealed partial class SubstationComponent : Component
{

    [ViewVariables(VVAccess.ReadOnly)]
    public float LastIntegrity = 100f;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool DecayEnabled = true;
   
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public SubstationIntegrityState State = SubstationIntegrityState.Healthy;

    //9.231205828 is the amount of moles in a 5L container (the default fuse) at 1000Kpa 20C°
    public float InitialFuseMoles = 2.051379050f;

}
