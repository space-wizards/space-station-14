using Robust.Shared.GameStates;

namespace Content.Shared.Follower.Components;

[RegisterComponent]
[Friend(typeof(FollowerSystem))]
[NetworkedComponent]
public sealed class FollowerComponent : Component
{
    public EntityUid Following;
}
