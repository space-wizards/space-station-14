using Robust.Shared.GameStates;

namespace Content.Shared.Follower.Components;

/// <summary>
/// Makes an entity unable to be followed.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DenyFollowComponent : Component
{

}
