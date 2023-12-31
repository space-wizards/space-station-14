using Content.Shared.Power;

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

    //9.231205828 is the amount of moles in a 5L container (the default conduit) at 1000Kpa 20C°
    public float InitialConduitMoles = 2.051379050f;

    [DataField("conduitSlotId", required: true)]
    [ViewVariables(VVAccess.ReadOnly)]
    public string ConduitSlotId = string.Empty;

    public bool AllowInsert = true;

}
