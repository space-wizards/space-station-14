using Content.Shared.Teleportation.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Teleportation.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedTeleportLocationsSystem)), AutoGenerateComponentState]
public sealed partial class TeleportLocationsComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<TeleportPoint> AvailableWarps = new();
}

[Serializable, NetSerializable]
public record struct TeleportPoint(string Location, NetEntity WarpPoint)
{
    public string Location = Location;
    public NetEntity WarpPoint = WarpPoint;
}
