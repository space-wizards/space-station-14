using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Follower.Components;

[RegisterComponent]
[Friend(typeof(FollowerSystem))]
[NetworkedComponent]
public sealed class FollowerComponent : Component
{
    public EntityUid Following;
}

[Serializable, NetSerializable]
public sealed class FollowerComponentState : ComponentState
{
    public EntityUid Following;

    public FollowerComponentState(EntityUid following)
    {
        Following = following;
    }
}
