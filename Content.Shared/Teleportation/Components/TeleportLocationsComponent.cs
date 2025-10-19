using Content.Shared.Teleportation.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Teleportation.Components;

// TODO: In the future assimilate ghost UI to use this.
/// <summary>
/// Used where you want an entity to display a list of player-safe teleport locations
/// They teleport to the location clicked
/// Looks for non Ghost-Only WarpPointComponents
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedTeleportLocationsSystem)), AutoGenerateComponentState]
public sealed partial class TeleportLocationsComponent : Component
{
    /// <summary>
    /// List of available warp points
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<TeleportPoint> AvailableWarps = new();

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
    /// Name of the Teleport Location menu
    /// </summary>
    [DataField]
    public LocId Name;

    /// <summary>
    /// Should the user have some speech if they teleport?
    /// If enabled it will be prepended to the location name.
    /// So something like "I am going to" would become "I am going to (Bridge)"
    /// </summary>
    [DataField]
    public LocId? Speech;
}

/// <summary>
/// A teleport point, which has a location (the destination) and the entity that it represents.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public partial record struct TeleportPoint
{
    [DataField]
    public string Location;
    [DataField]
    public NetEntity TelePoint;

    public TeleportPoint(string Location, NetEntity TelePoint)
    {
        this.Location = Location;
        this.TelePoint = TelePoint;
    }
}
