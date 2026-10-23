using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.Storage.Components;

/// <summary>
/// Applies an ongoing pickup area around the attached entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[AutoGenerateComponentPause]
public sealed partial class MagnetPickupComponent : Component
{
    /// <summary>
    /// Whether automatic pickup is enabled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField]
    [AutoPausedField]
    [AutoNetworkedField]
    public TimeSpan NextScan = TimeSpan.Zero;

    /// <summary>
    /// What container slot the magnet needs to be in to work.
    /// </summary>
    [DataField]
    public SlotFlags? SlotFlags = Inventory.SlotFlags.BELT;

    [DataField]
    public bool RequireActiveHand = false;

    /// <summary>
    /// Whether the magnet must be anchored
    /// </summary>
    [DataField]
    public bool RequireAnchored = false;

    [DataField]
    public float Range = 1f;
}

/// <summary>
/// Raised on a magnet to insert a nearby item
/// </summary>
[ByRefEvent]
public record struct MagnetPickupAttemptEvent(EntityUid Item, bool PlaySound = true)
{
    public bool Handled;
}
