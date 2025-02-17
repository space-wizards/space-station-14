// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Storage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Audio;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Abilities.Egg.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class EggComponent : Component
{
    [DataField("actionHatch", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionHatch = "ActionHatch";

    [DataField("actionHatchEntity")]
    public EntityUid? ActionHatchEntity;

    [DataField("timeUntilSpawn", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan TimeUntilSpawn;

    [DataField("duration")]
    public float Duration = 60f;

    [DataField("spawned", required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<EntitySpawnEntry> SpawnedEntities = new();

    [DataField("durationPlayEggSound")]
    public float DurationPlayEggSound = 10f;

    [DataField("timeUntilPlaySound", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan TimeUntilPlaySound;

    [DataField("eggSound")]
    public SoundSpecifier? EggSound = default;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "Egg";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool CanHatch = false;

    public override bool SessionSpecific => true;
}

[ByRefEvent]
public readonly record struct EggSpawnEvent();

[ByRefEvent]
public readonly record struct PlayEggSoundEvent();
