using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.Storage.Components;

/// <summary>
/// Applies an ongoing pickup area around the attached entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class MagnetPickupComponent : Component
{
    [DataField, AutoPausedField]
    public TimeSpan NextScan = TimeSpan.Zero;

    /// <summary>
    /// What container slot the magnet needs to be in to work.
    /// </summary>
    [DataField]
    public SlotFlags SlotFlags = SlotFlags.BELT;

    /// <summary>
    /// Does it work while held in hand? (False by default)
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool MagnetInHands;

    /// <summary>
    /// Can only magnet stuff when placed correctly.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool InRightPlace;

    [DataField]
    public float Range = 1f;
}
