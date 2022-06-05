using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Follower.Components;

/// <summary>
///     Attached to entities that are currently being followed by a ghost.
/// </summary>
[RegisterComponent, Friend(typeof(FollowerSystem))]
[NetworkedComponent]
public sealed class FollowedComponent : Component
{
    public HashSet<EntityUid> Followers = new();
}

[Serializable, NetSerializable]
public sealed class FollowedComponentState : ComponentState
{
    public HashSet<EntityUid> Followers;

    public FollowedComponentState(HashSet<EntityUid> followers)
    {
        Followers = followers;
    }
}
