using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Robust.Shared.Network;

namespace Content.Server.KillTracking;

/// <summary>
/// This is used for entities that track player damage sources and killers.
/// </summary>
[RegisterComponent]
public sealed partial class KillTrackerComponent : Component
{
    /// <summary>
    /// The mobstate that registers as a "kill"
    /// </summary>
    [DataField("killState")]
    public MobState KillState = MobState.Critical;

    /// <summary>
    /// A dictionary of sources and how much damage they've done to this entity over time.
    /// </summary>
    [DataField("lifetimeDamage")]
    public Dictionary<KillSource, FixedPoint2> LifetimeDamage = new();
}

public abstract record KillSource;

[DataDefinition, Serializable]
public sealed partial record KillPlayerSource(NetUserId PlayerId) : KillSource
{
    [DataField("playerId")]
    public NetUserId PlayerId { get; } = PlayerId;
}

[DataDefinition, Serializable]
public sealed partial record KillNpcSource(EntityUid NpcEnt) : KillSource
{
    [DataField("npcEnt")]
    public EntityUid NpcEnt { get; } = NpcEnt;
}

[DataDefinition, Serializable]
public sealed partial record KillEnvironmentSource : KillSource;
