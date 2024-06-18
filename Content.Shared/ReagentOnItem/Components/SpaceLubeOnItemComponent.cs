using Robust.Shared.GameStates;

namespace Content.Shared.ReagentOnItem;

[RegisterComponent, NetworkedComponent]
public sealed partial class SpaceLubeOnItemComponent : ReagentOnItemComponent
{

    /// <summary>
    ///     Probability to reduce the amount of reagent after a grab.
    /// </summary>
    [DataField]
    public double ChanceToDecreaseReagentOnGrab = .45;

    /// <summary>
    ///     How far will the item be thrown when someone tries to pick it up
    ///     while it has lube applied to it.
    /// </summary>
    [DataField]
    public float PowerOfThrowOnPickup = 10f;
}
