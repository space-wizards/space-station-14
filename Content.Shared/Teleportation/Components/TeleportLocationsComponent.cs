using Content.Shared.Teleportation.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Teleportation.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedTeleportLocationsSystem)), AutoGenerateComponentState]
public sealed partial class TeleportLocationsComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<TeleportPoint> AvailableWarps = new();

    /// <summary>
    ///     User of this
    /// </summary>
    [DataField]
    public EntityUid? User;

    /// <summary>
    ///     What should spawn as an effect when the user teleports?
    /// </summary>
    [DataField]
    public EntProtoId? TeleportEffect;

    /// <summary>
    ///     Should this close the BUI after teleport?
    /// </summary>
    [DataField]
    public bool CloseAfterTeleport;

    /// <summary>
    ///     Should the user speak on teleport?
    /// </summary>
    [DataField]
    public string Speech = "";
}

[Serializable, NetSerializable]
public record struct TeleportPoint(string Location, NetEntity WarpPoint)
{
    public string Location = Location;
    public NetEntity WarpPoint = WarpPoint;
}
