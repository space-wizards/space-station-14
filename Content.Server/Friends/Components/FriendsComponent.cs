using Content.Server.Friends.Systems;

namespace Content.Server.Friends.Components;

[RegisterComponent, Access(typeof(FriendsSystem))]
public sealed class FriendsComponent : Component
{
    /// <summary>
    /// List of entities that this NPC will refuse to attack
    /// </summary>
    [DataField("friends")]
    public HashSet<EntityUid> Friends = new();

    /// <summary>
    /// If true, this entity can be petted (press Z) to add the user to its friends.
    /// </summary>
    [DataField("pettable"), ViewVariables(VVAccess.ReadWrite)]
    public bool Pettable;
}
