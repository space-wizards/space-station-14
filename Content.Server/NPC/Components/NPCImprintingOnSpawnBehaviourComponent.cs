using Content.Shared.Whitelist;
using Robust.Shared.Collections;

namespace Content.Server.NPC.Components;
/// <summary>
/// A component that makes the entity friendly to nearby creatures it sees on init.
/// </summary>
[RegisterComponent]
public sealed partial class NPCImprintingOnSpawnBehaviourComponent : Component
{
    /// <summary>
    /// filter who can be a friend to this creature
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// when a creature appears, it will memorize all creatures in the radius to remember them as friends
    /// </summary>
    [DataField]
    public float SpawnFriendsSearchRadius = 3f;

    /// <summary>
    /// if there is a FollowCompound in HTN, the target of the following will be selected from random nearby targets when it appears
    /// </summary>
    [DataField]
    public bool Follow = true;

    /// <summary>
    /// is used to determine who became a friend from this component
    /// </summary>
    [DataField]
    public List<EntityUid> Friends = new();
}
