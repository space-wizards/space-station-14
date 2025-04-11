using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Store.Components;

/// <summary>
/// This component allows a store to steal, as well as get stolen from.
/// This means that using this entity on another entity will transfer all currencies after the doAfter is complete.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StealableStoreComponent : Component
{
    /// <summary>
    /// How long will it take to steal the currencies from this store?
    /// </summary>
    [DataField]
    public TimeSpan DoAfterDuration = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Whether this store can be stolen from by another store.
    /// </summary>
    [DataField]
    public bool CanBeStolenFrom = true;

    /// <summary>
    /// Should the target store be unlocked to steal currency from it?
    /// Used in PDA uplinks.
    /// </summary>
    [DataField]
    public bool RequireTargetUnlocked = false;

    /// <summary>
    /// Should the used store be unlocked to allow to steal currency?
    /// Used in PDA uplinks.
    /// </summary>
    [DataField]
    public bool RequireUserUnlocked = true;

    /// <summary>
    /// Popup to show to the user when they begin stealing from another store.
    /// </summary>
    [DataField]
    public LocId? SelfStealPopup = "store-steal-self-popup";

    /// <summary>
    /// Popup to show to everyone around once a store has been stolen from.
    /// </summary>
    [DataField]
    public LocId? SuccessfulStealPopup = "store-steal-success-popup";

    /// <summary>
    /// The sound played around the player when currency has successfully been stolen.
    /// </summary>
    [DataField]
    public SoundSpecifier FinishStealingSound = new SoundPathSpecifier("/Audio/Effects/kaching.ogg");
}
