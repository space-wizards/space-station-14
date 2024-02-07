using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
public sealed class ShuttleBoundUserInterfaceState : BoundUserInterfaceState
{
    public NavInterfaceState NavState;
    public ShuttleMapInterfaceState MapState;
    public DockingInterfaceState DockState;

    public ShuttleBoundUserInterfaceState(NavInterfaceState navState, ShuttleMapInterfaceState mapState, DockingInterfaceState dockState)
    {
        NavState = navState;
        MapState = mapState;
        DockState = dockState;
    }
}

[Serializable, NetSerializable]
public readonly record struct ShuttleBeacon(NetEntity Entity, NetCoordinates Coordinates, string Name);

[Serializable, NetSerializable]
public record struct ShuttleExclusion(NetCoordinates Coordinates, float Range);
