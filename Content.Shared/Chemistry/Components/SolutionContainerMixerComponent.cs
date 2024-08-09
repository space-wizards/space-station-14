using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// This is used for an entity that uses <see cref="ReactionMixerComponent"/> to mix any container with a solution after a period of time.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSolutionContainerMixerSystem))]
public sealed partial class SolutionContainerMixerComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string ContainerId = "mixer";

    [DataField, AutoNetworkedField]
    public bool Mixing;

    /// <summary>
    /// How long it takes for mixing to occurs.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan MixDuration;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan MixTimeEnd;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? MixingSound;

    [DataField]
    public Entity<AudioComponent>? MixingSoundEntity;
}

[Serializable, NetSerializable]
public enum SolutionContainerMixerVisuals : byte
{
    Mixing
}
