using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.Storage.Components;

/// <summary>
/// Applies a pickup area around the attached entity.  Can be automatic or manual.
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

    /// <summary>
    /// Whether this requires a manual trigger instead of being automatic.
    /// </summary>
    [DataField]
    public bool ManualTriggerOnly = false;

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
    /// Whether the magnet needs power to work.
    /// </summary>
    [DataField]
    public bool RequirePowered = false;

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
