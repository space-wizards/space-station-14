using Robust.Shared.GameStates;

namespace Content.Shared.ReagentOnItem;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class SpaceLubeOnItemComponent : ReagentOnItemComponent
{
    /// <summary>
    ///     Probability to reduce the amount of reagent after a grab.
    /// </summary>
    [DataField]
    public double ChanceToDecreaseReagentOnGrab = .45;

    /// <summary>
    ///     How far will the item be thrown when someone tries to pick it up while it has lube applied to it.
    /// </summary>
    [DataField]
    public float PowerOfThrowOnPickup = 5f;

    /// <summary>
    ///     Time that the item can't be picked up after someone tries to grab it.
    /// </summary>
    /// <remarks>
    ///     If this is too low, you can try to pick the item up multiple times in "one" click. Do not lower it without
    ///     testing!
    /// </remarks>
    [DataField]
    public TimeSpan PickupCooldown = TimeSpan.FromSeconds(0.5);

    /// <summary>
    ///     The last time someone tried to pick up the lubed item.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan LastTimeAttemptedPickup;
}
