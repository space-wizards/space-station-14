using Robust.Shared.GameStates;

namespace Content.Shared.AccessBreaker;

/// <summary>
/// Marker component for breaking access and locks
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AccessBrokenComponent : Component
{

}
