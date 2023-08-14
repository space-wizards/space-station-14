using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Robust.Shared.Network;

namespace Content.Server.KillTracking;

/// <summary>
/// This is used for entities that track player damage sources and killers.
/// </summary>
[RegisterComponent]
public sealed class KillTrackerComponent : Component
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
public sealed record KillPlayerSource(NetUserId PlayerId) : KillSource
{
    [DataField("playerId")]
    public readonly NetUserId PlayerId = PlayerId;
}

[DataDefinition, Serializable]
public sealed record KillNpcSource(EntityUid NpcEnt) : KillSource
{
    [DataField("npcEnt")]
    public readonly EntityUid NpcEnt = NpcEnt;
}

[DataDefinition, Serializable]
public sealed record KillEnvironmentSource : KillSource;
