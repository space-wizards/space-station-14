using Content.Shared.NPC.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.NPC.Components;

/// <summary>
/// Prevents an NPC from attacking ignored entities from enemy factions.
/// Can be added to if pettable, see PettableFriendComponent.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(NpcFactionSystem))]
public sealed partial class FactionExceptionComponent : Component
{
    /// <summary>
    /// Collection of entities that this NPC will refuse to attack
    /// </summary>
    [DataField]
    public HashSet<EntityUid> Ignored = new();

    /// <summary>
    /// Collection of entities that this NPC will attack, regardless of faction.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> Hostiles = new();
}
