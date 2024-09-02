using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;
using Robust.Shared.Audio;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// Artifact polymorphs surrounding entities when triggered.
/// </summary>
[RegisterComponent]
[Access(typeof(PolyOthersArtifactSystem))]
public sealed partial class PolyOthersArtifactComponent : Component
{
    /// <summary>
    /// The polymorph effect to trigger.
    /// </summary>
    [DataField]
    public ProtoId<PolymorphPrototype> PolymorphPrototypeName = "ArtifactMonkey";

    /// <summary>
    /// range of the effect.
    /// </summary>
    [DataField]
    public float Range = 2f;

    /// <summary>
    /// Sound to play on polymorph.
    /// </summary>
    [DataField]
    public SoundSpecifier PolySound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/Magic/staff_animation.ogg");
}
