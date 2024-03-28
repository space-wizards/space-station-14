using Content.Shared.NPC.Systems;
using Content.Shared.Whitelist;

namespace Content.Shared.NPC.Components;
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
    public EntityWhitelist Whitelist;

    /// <summary>
    /// the radius in which all creatures will be marked as exceptions to attack
    /// </summary>
    [DataField]
    public float SearchRadius = 3f;

    /// <summary>
    /// if there is a FollowCompound in HTN, the target of the following will be selected from random nearby targets when it appears
    /// </summary>
    [DataField]
    public bool Follow = true;
}
