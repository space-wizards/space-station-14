using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
public sealed class DockingInterfaceState(Dictionary<NetEntity, List<DockingPortState>> docks)
{
    public Dictionary<NetEntity, List<DockingPortState>> Docks = docks;
}
