namespace Content.Shared.ReagentOnItem;

[RegisterComponent]
public sealed partial class SpaceLubeOnItemComponent : ReagentOnItemComponent
{
    [DataField("chanceToDecreaseReagentOnGrab"), ViewVariables(VVAccess.ReadWrite)]
    public double ChanceToDecreaseReagentOnGrab = .60;

    [DataField("powerOfThrowOnPickup"), ViewVariables(VVAccess.ReadWrite)]
    public float PowerOfThrowOnPickup = 10f;
}
