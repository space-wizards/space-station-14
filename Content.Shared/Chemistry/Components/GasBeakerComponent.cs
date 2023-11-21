
namespace Content.Shared.Chemistry.Components;

[RegisterComponent]
public sealed partial class GasBeakerComponent : Component
{
    [DataField("tankSlotId", required: true)]
    [ViewVariables(VVAccess.ReadOnly)]
    public string TankSlotId = string.Empty;
}