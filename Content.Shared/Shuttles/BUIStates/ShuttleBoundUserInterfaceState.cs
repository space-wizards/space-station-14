using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
public sealed class ShuttleBoundUserInterfaceState(
    NavInterfaceState navState,
    ShuttleMapInterfaceState mapState,
    DockingInterfaceState dockState)
    : BoundUserInterfaceState
{
    public NavInterfaceState NavState = navState;
    public ShuttleMapInterfaceState MapState = mapState;
    public DockingInterfaceState DockState = dockState;
}
