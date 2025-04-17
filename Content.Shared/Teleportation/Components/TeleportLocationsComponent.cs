using Content.Shared.Teleportation.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Teleportation.Components;

/// <summary>
/// Used where you want an entity to display a list of player-safe teleport locations
/// They teleport to the location clicked
/// Looks for non Ghost-Only WarpPointComponents
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedTeleportLocationsSystem)), AutoGenerateComponentState]
public sealed partial class TeleportLocationsComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<TeleportPoint> AvailableWarps = new();

    /// <summary>
    /// User of this
    /// </summary>
    [DataField]
    public EntityUid? User;

    /// <summary>
    /// What should spawn as an effect when the user teleports?
    /// </summary>
    [DataField]
    public EntProtoId? TeleportEffect;

    /// <summary>
    /// Should this close the BUI after teleport?
    /// </summary>
    [DataField]
    public bool CloseAfterTeleport;

    /// <summary>
    /// Should the user have some speech if they teleport?
    /// If enabled it will be prepended to the location name.
    /// So something like "I am going to" would become "I am going to (Bridge)"
    /// </summary>
    [DataField]
    public string Speech = "";
}

/// <summary>
/// A teleport point, which has a location (the destination) and the entity that it represents.
/// </summary>
[Serializable, NetSerializable]
public record struct TeleportPoint(string Location, NetEntity TelePoint)
{
    public string Location = Location;
    public NetEntity TelePoint = TelePoint;
}
