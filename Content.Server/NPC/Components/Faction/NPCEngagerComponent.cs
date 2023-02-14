using Content.Server.NPC.Systems.Faction;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.NPC.Components.Faction;

/// <summary>
/// Inverse of <see cref="NPCCombatTargetComponent"/>, this is added when this entity has damaged an NPC.
/// </summary>
[RegisterComponent]
[Access(typeof(NPCCombatTargetSystem))]
public sealed class NPCEngagerComponent : Component
{
    /// <summary>
    /// How long this component lasts before removing itself if it's not refreshed.
    /// </summary>
    [DataField("decay")]
    public TimeSpan Decay = TimeSpan.FromSeconds(7);

    [DataField("nextChargeTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? RemoveWhen;

    /// <summary>
    /// Which entities we are trying to kill right now...
    /// </summary>
    [DataField("engagedEntities")]
    public HashSet<EntityUid> EngagedEnemies = new();
}
