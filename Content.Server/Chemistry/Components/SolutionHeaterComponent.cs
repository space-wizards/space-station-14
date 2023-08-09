namespace Content.Server.Chemistry.Components;

[RegisterComponent]
public sealed class SolutionHeaterComponent : Component
{
    public readonly string BeakerSlotId = "beakerSlot";

    [DataField("heatPerSecond")]
    public float HeatPerSecond = 120;

    [ViewVariables(VVAccess.ReadWrite)]
    public float HeatMultiplier = 1;

    [DataField("machinePartHeatPerSecond")]
    public string MachinePartHeatPerSecond = "Laser";

    [DataField("partRatingHeatMultiplier")]
    public float PartRatingHeatMultiplier = 1.5f;
}
