using Content.Server.Friends.Systems;

namespace Content.Server.Friends.Components;

/// <summary>
/// NPCs with friends wont attack their friends.
/// Can be added to if pettable, see PettableFriendComponent.
/// </summary>
[RegisterComponent, Access(typeof(FriendsSystem))]
public sealed class FriendsComponent : Component
{
    /// <summary>
    /// List of entities that this NPC will refuse to attack
    /// </summary>
    [DataField("friends")]
    public HashSet<EntityUid> Friends = new();
}
