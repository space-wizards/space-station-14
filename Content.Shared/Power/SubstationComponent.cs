namespace Content.Shared.Power;

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

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool MaintenanceDoorOpen = false;

    // 9.231205828 is the amount of moles in a 5L container (the default conduit) at 1000Kpa 20C°
    public float InitialNitrogenBoosterMoles = 2.051379050f;

    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadOnly)]
    public string NitrogenBoosterSlotId = string.Empty;

    public bool AllowInsert = true;

    public float SubstationLightBlinkInterval = 1f;

    public float SubstationLightBlinkTimer = 1f;

    public bool SubstationLightBlinkState = true;
}
