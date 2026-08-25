using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.CosmicCult.Components;

/// <summary>
/// Component for Cosmic Cult's Vacuous Chantry.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class CosmicChantryComponent : Component
{
    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan SpawnTimer = default!;

    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan CountdownTimer = default!;

    [DataField] public TimeSpan SpawningTime = TimeSpan.FromSeconds(0.9f);

    [DataField] public TimeSpan EventTime = TimeSpan.FromSeconds(150);

    [DataField] public bool Spawned;

    [DataField] public bool Completed;

    [DataField] public EntityUid InternalVictim;

    [DataField] public EntityUid VictimBody;

    [DataField] public SoundSpecifier ChantryAlarm = new SoundPathSpecifier("/Audio/_ST/CosmicCult/chantry_alarm.ogg");

    [DataField] public EntProtoId Colossus = "MobCosmicColossus";

    [DataField] public EntProtoId FallbackBrain = "CosmicCultMindSink";

    [DataField] public EntProtoId SpawnVfx = "CosmicGlareAbilityVfx";

    [DataField] public EntProtoId FallbackVfx = "CosmicGenericVfx";
}

[Serializable, NetSerializable]
public enum ChantryVisuals : byte
{
    Status,
}

[Serializable, NetSerializable]
public enum ChantryStatus : byte
{
    Off,
    On,
}
