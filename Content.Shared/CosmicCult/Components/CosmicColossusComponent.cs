using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.CosmicCult.Components;

/// <summary>
/// Component for Cosmic Cult's entropic colossus.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class CosmicColossusComponent : Component
{
    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan AttackHoldTimer = default!;

    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan HibernationTimer = default!;

    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan DeathTimer = default!;

    [DataField] public SoundSpecifier ReawakenSfx = new SoundPathSpecifier("/Audio/_ST/CosmicCult/colossus_spawn.ogg");

    [DataField] public SoundSpecifier DeathSfx = new SoundPathSpecifier("/Audio/_ST/CosmicCult/colossus_death.ogg");

    [DataField] public SoundSpecifier IngressSfx = new SoundPathSpecifier("/Audio/_ST/CosmicCult/Abilities/ability-ingress.ogg");

    [DataField] public SoundSpecifier DoAfterSfx = new SoundPathSpecifier("/Audio/Machines/airlock_creaking.ogg");

    [DataField] public EntProtoId CultVfx = "CosmicGenericVfx";

    [DataField] public EntProtoId CultBigVfx = "CosmicGlareAbilityVfx";

    [DataField] public EntProtoId Attack1Vfx = "CosmicColossusAttack1Vfx";

    [DataField] public EntProtoId TileDetonations = "MobTileDamageZone";

    [DataField] public TimeSpan IngressDoAfter = TimeSpan.FromSeconds(4);

    [DataField] public TimeSpan AttackWait = TimeSpan.FromSeconds(1.5);

    [DataField] public TimeSpan HibernationWait = TimeSpan.FromSeconds(20);

    [DataField] public TimeSpan DeathWait = TimeSpan.FromMinutes(15);

    [DataField] public bool Attacking;

    [DataField] public bool Hibernating;

    [DataField] public bool Timed;
}

[Serializable, NetSerializable]
public enum ColossusVisuals : byte
{
    Status,
    Hibernation,
    Sunder,
}

[Serializable, NetSerializable]
public enum ColossusStatus : byte
{
    Alive,
    Dead,
    Action,
}

[Serializable, NetSerializable]
public enum ColossusAction : byte
{
    Running,
    Stopped,
}
