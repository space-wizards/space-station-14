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
    ///
    /// </summary>
    [DataField]
    public EntityUid? TeleLocOwner;

    /// <summary>
    ///     What should spawn as an effect when the user teleports?
    /// </summary>
    [DataField]
    public EntProtoId? TeleportEffect;
}

[Serializable, NetSerializable]
public record struct TeleportPoint(string Location, NetEntity WarpPoint)
{
    public string Location = Location;
    public NetEntity WarpPoint = WarpPoint;
}
