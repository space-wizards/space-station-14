using Robust.Shared.Audio;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// Artifact polymorphs surrounding entities when triggered.
/// </summary>
[RegisterComponent]
public sealed partial class PolyArtifactComponent : Component
{
    /// <summary>
    /// range of the effect.
    /// </summary>
    [DataField("range")]
    public float Range = 2f;

    /// <summary>
    /// Sound to play on polymorph.
    /// </summary>
    [DataField("polySound")]
    public SoundSpecifier PolySound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/Magic/staff_animation.ogg");
}
