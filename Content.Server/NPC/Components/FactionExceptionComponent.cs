using Content.Server.NPC.Systems;

namespace Content.Server.NPC.Components;

/// <summary>
/// Prevents an NPC from attacking ignored entities from enemy factions.
/// Can be added to if pettable, see PettableFriendComponent.
/// </summary>
[RegisterComponent, Access(typeof(NpcFactionSystem))]
public sealed partial class FactionExceptionComponent : Component
{
    /// <summary>
    /// Collection of entities that this NPC will refuse to attack
    /// </summary>
    [DataField("ignored")]
    public HashSet<EntityUid> Ignored = new();

    /// <summary>
    /// Collection of entities that this NPC will attack, regardless of faction.
    /// </summary>
    [DataField("hostiles")]
    public HashSet<EntityUid> Hostiles = new();
}
