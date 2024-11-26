using Content.Shared.Audio;
using Robust.Shared.Audio;

namespace Content.Server.Gatherable.Components;

/// <summary>
/// Plays the specified sound when this entity is gathered.
/// </summary>
[RegisterComponent, Access(typeof(GatherableSystem))]
public sealed partial class SoundOnGatherComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("sound")]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("GatherPickupSound")
    {
        Params = AudioParams.Default
            .WithVariation(SharedContentAudioSystem.DefaultVariation)
            .WithVolume(-3f),
    };
}
