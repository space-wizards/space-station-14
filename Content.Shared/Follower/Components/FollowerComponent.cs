using Robust.Shared.GameStates;

namespace Content.Shared.Follower.Components;

[RegisterComponent]

[NetworkedComponent]
public sealed class FollowerComponent : Component
{
    public EntityUid Following;
}
