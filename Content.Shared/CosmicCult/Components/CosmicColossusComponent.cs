using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.CosmicCult.Components;

/// <summary>
/// Component for Cosmic Cult's entropic colossus.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class CosmicColossusComponent : Component
{
    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? HibernationTimer;

    [DataField]
    public EntProtoId SunderVfx = "EffectCosmicActionSunder";

    [DataField] public SoundSpecifier ReawakenSfx = new SoundPathSpecifier("/Audio/Cosmic/colossus-spawn.ogg");

    [DataField] public SoundSpecifier DeathSfx = new SoundPathSpecifier("/Audio/Cosmic/colossus-death.ogg");

    [DataField] public EntProtoId CultVfx = "EffectCosmicBigWindup";

    [DataField] public EntProtoId CultBigVfx = "EffectCosmicActionGlare";

    [DataField] public bool AnimReady;
}

[Serializable, NetSerializable]
public enum ColossusVisuals : byte
{
    Visuals,
}

[Serializable, NetSerializable]
public enum ColossusStatus : byte
{
    Alive,
    Dead,
    Sunder,
    Hibernate,
}
