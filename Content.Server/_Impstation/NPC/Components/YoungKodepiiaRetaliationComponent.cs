using Content.Server._Impstation.NPC.Systems;

namespace Content.Server._Impstation.NPC.Components;

/// <summary>
/// Entities with this component will retaliate against those who interact with them.
/// It has an optional "memory" specification wherein it will only attack those entities for a specified length of time.
/// This is wholesale copied from NPCRetaliationComponent
/// </summary>
[RegisterComponent, Access(typeof(YoungKodepiiaRetaliationSystem))]
public sealed partial class YoungKodepiiaRetaliationComponent : Component
{
    /// <summary>
    /// How long after being attacked will an NPC continue to be aggressive to the attacker for.
    /// </summary>
    [DataField("attackMemoryLength"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? AttackMemoryLength;

    /// <summary>
    /// A dictionary that stores an entity and the time at which they will no longer be considered hostile.
    /// </summary>
    /// todo: this needs to support timeoffsetserializer at some point
    [DataField("attackMemories")]
    public Dictionary<EntityUid, TimeSpan> AttackMemories = new();
}
