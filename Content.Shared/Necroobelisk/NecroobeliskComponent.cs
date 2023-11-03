using System.Numerics;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Necroobelisk.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class NecroobeliskComponent : Component
{
    #region Sanity

    [DataField("rangesanity")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float RangeSanity = 15f;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan CheckDurationSanity = TimeSpan.FromSeconds(1);

    [DataField("nextCheckTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextCheckTimeSanity = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite), DataField("sanityMobSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SanityMobSpawnId = "MobSlasher";

    #endregion

    #region Pulse
    /// <summary>
    /// The time at which the next artifact pulse will occur.
    /// </summary>
    [DataField("nextPulseTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextPulseTime = TimeSpan.Zero;

    /// <summary>
    /// The minimum interval between pulses.
    /// </summary>
    [DataField("minPulseLength")]
    public TimeSpan MinPulseLength = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The maximum interval between pulses.
    /// </summary>
    [DataField("maxPulseLength")]
    public TimeSpan MaxPulseLength = TimeSpan.FromSeconds(30);

    /// <summary>
    /// A percentage by which the length of a pulse might vary.
    /// </summary>
    [DataField("pulseVariation")]
    public float PulseVariation = 0.1f;

    [DataField("pulselvl")]
    public int Pulselvl = 0;
    /// <summary>
    /// The range that an anomaly's stability can vary each pulse. Scales with severity.
    /// </summary>
    /// <remarks>
    /// This is more likely to trend upwards than donwards, because that's funny
    /// </remarks>
    [DataField("pulseStabilityVariation")]
    public Vector2 PulseStabilityVariation = new(-0.1f, 0.15f);


    /// <summary>
    /// The maximum radius of tiles scales with stability
    /// </summary>
    [DataField("spawnRange"), ViewVariables(VVAccess.ReadWrite)]
    public float SpawnRange = 5f;

    /// <summary>
    /// The tile that is spawned by the Necroobelisk's effect
    /// </summary>
    [DataField("floorTileId", customTypeSerializer: typeof(PrototypeIdSerializer<ContentTileDefinition>)), ViewVariables(VVAccess.ReadWrite)]
    public string FloorTileId = "NecroFlesh";

    /// <summary>
    /// The entity spawned when the anomaly goes supercritical
    /// </summary>
    [DataField("superCriticalSpawn", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string SupercriticalSpawn = "NecroKudzu";

    #endregion

    [DataField("active")]
    public int Active = 1;

    #region Visualizer
    [DataField("state")]
    public string State = "active";
    [DataField("unactiveState")]
    public string UnactiveState = "unactive";
    #endregion

}

[NetSerializable, Serializable]
public enum RevenantVisuals : byte
{
    Active,
    Unactive
}


[Serializable, NetSerializable]
public sealed class NecroobeliskComponentState : ComponentState
{
    public TimeSpan NextPulseTime;
}

[ByRefEvent]

public readonly record struct NecroobeliskCheckStateEvent(TimeSpan CurTime);

[ByRefEvent]

public readonly record struct NecroobeliskPulseEvent();

[ByRefEvent]

public readonly record struct NecroobeliskSpawnArmyEvent();

[Serializable, NetSerializable]
public sealed class SanityComponentState : ComponentState
{
    public TimeSpan NextCheckTime;
}

[ByRefEvent]
public readonly record struct SanityCheckEvent(EntityUid victinUID);
