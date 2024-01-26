namespace Content.Client.Chemistry.Components;

[RegisterComponent]
public sealed partial class MedipenRefillerComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string BufferSolutionName = "buffer";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string InputSlotName = "beakerSlot";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string MedipenSlotName = "medipenSlot";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string MedipenSolutionName = "pen";
}
