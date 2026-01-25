using Robust.Shared.GameStates;

namespace Content.Shared.Follower.Components;

// TODO properly network this and followercomp.
/// <summary>
///     Attached to entities that are currently being followed by a ghost.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(FollowerSystem))]
public sealed partial class FollowedComponent : Component
{
    public override bool SessionSpecific => true;

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Following = new();
}
