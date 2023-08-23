using Robust.Shared.GameStates;

namespace Content.Shared.Follower.Components;

// TODO properly network this and followercomp.
/// <summary>
///     Attached to entities that are currently being followed by a ghost.
/// </summary>
[RegisterComponent, Access(typeof(FollowerSystem))]
[NetworkedComponent]
public sealed partial class FollowedComponent : Component
{
    [DataField("following")]
    public HashSet<EntityUid> Following = new();
}
