using Robust.Shared.GameStates;

namespace Content.Shared.Follower.Components;

// TODO properly network this and followercomp.
/// <summary>
///     Attached to entities that are currently being followed by a ghost.
/// </summary>
[RegisterComponent, Access(typeof(FollowerSystem))]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FollowedComponent : Component
{
    [AutoNetworkedField(true), DataField("following")]
    public HashSet<EntityUid> Following = new();
}
