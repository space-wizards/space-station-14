// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Storage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Audio;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Abilities.Egg.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class EggComponent : Component
{
    [DataField]
    public EntProtoId ActionHatch = "ActionHatch";

    [DataField]
    public EntityUid? ActionHatchEntity;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan TimeUntilSpawn;

    [DataField]
    public float Duration = 60f;

    [DataField("spawned")]
    public List<EntitySpawnEntry> SpawnedEntities = new()
    {
        new EntitySpawnEntry
        {
            PrototypeId = "MobSpiderTerrorGuardian",
            SpawnProbability = 1.0f,
            GroupId = null,
            Amount = 3,
            MaxAmount = 3
        }
    };

    [DataField]
    public float DurationPlayEggSound = 10f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan TimeUntilPlaySound;

    [DataField]
    public SoundSpecifier? EggSound = default;

    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "Egg";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool CanHatch = false;

    public override bool SessionSpecific => true;
}

[ByRefEvent]
public readonly record struct EggSpawnEvent();

[ByRefEvent]
public readonly record struct PlayEggSoundEvent();
