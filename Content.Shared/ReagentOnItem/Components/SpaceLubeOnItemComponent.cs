namespace Content.Shared.ReagentOnItem;

[RegisterComponent]
public sealed partial class SpaceLubeOnItemComponent : ReagentOnItemComponent
{

    /// <summary>
    /// Probability to reduce the amount of reagent after a grab.
    /// </summary>
    [DataField("chanceToDecreaseReagentOnGrab"), ViewVariables(VVAccess.ReadWrite)]
    public double ChanceToDecreaseReagentOnGrab = .35;

    /// <summary>
    /// How far will the item be thrown when someone tries to pick it up
    /// while it has lube applied to it.
    /// </summary>
    [DataField("powerOfThrowOnPickup"), ViewVariables(VVAccess.ReadWrite)]
    public float PowerOfThrowOnPickup = 10f;
}
