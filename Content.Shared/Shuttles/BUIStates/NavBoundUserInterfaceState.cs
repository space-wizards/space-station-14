using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

/// <summary>
/// Wrapper around <see cref="NavInterfaceState"/>
/// </summary>
[Serializable, NetSerializable]
public sealed class NavBoundUserInterfaceState(NavInterfaceState state) : BoundUserInterfaceState
{
    public NavInterfaceState State = state;
}
