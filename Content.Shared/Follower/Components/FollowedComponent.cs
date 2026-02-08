using Robust.Shared.GameStates;

namespace Content.Shared.Follower.Components;

// TODO properly network this and followercomp.
/// <summary>
///     Attached to entities that are currently being followed by a ghost.
/// </summary>
[RegisterComponent, NetworkedComponent(StateRestriction.SessionSpecific), AutoGenerateComponentState]
[Access(typeof(FollowerSystem))]
public sealed partial class FollowedComponent : Component
{

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Following = new();
}
