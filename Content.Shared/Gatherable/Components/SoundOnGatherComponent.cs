using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Gatherable.Components;

/// <summary>
/// Plays the specified sound when this entity is gathered.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(GatherableSystem))]
public sealed partial class SoundOnGatherComponent : Component
{
    /// <summary>
    /// Sound to play when this entity is gathered.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/break_stone.ogg", AudioParams.Default
        .WithVariation(SharedContentAudioSystem.DefaultVariation)
        .WithVolume(-3f));
}
