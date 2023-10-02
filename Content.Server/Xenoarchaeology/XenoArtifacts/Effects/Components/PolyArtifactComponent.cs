using Robust.Shared.Audio;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// Artifact that polymorphs surrounding entities when triggered, Range handles the range of effect and sound is played on polymorph.
/// </summary>
[RegisterComponent]
public sealed partial class PolyArtifactComponent : Component
{
    [DataField("range")]
    public float Range = 2f;


    [DataField("polySound")]
    public SoundSpecifier PolySound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/Magic/staff_animation.ogg");
}
