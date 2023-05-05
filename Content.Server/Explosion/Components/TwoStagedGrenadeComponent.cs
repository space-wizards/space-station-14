using Content.Server.Singularity.Components;
using Content.Shared.Singularity.Components;
using Robust.Shared.Audio;

namespace Content.Server.Explosion.Components;

/// <summary>
/// This component after trigger starts timer to trigger for second time to explode or delete self.
/// This component is controlling process of exploding of supermatter grenade.
/// </summary>
[RegisterComponent]
public sealed class TwoStagedGrenadeComponent : Component
{
    /// <summary>
    /// Time after first trigger (i.e. how long will second stage take) that will pass and grenade will be triggered again.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("explosionDelay")]
    public float ExplosionDelay = 0f;

    public TimeSpan TimeOfExplosion = TimeSpan.Zero;

    /// <summary>
    /// Offset when AmbienceComponent will be enabled after first trigger.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("ambienceSoundOffset")]
    public float AmbienceSoundOffset = 0f;

    public TimeSpan AmbienceStartTime = TimeSpan.Zero;

    public bool IsSecondStageSoundBegan = false;

    /// <summary>
    /// true => After second stage grenade will explode. Require ExplosiveComponent.
    /// false => Grenade will just pull things in heap and delete itself.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("explodeAfterGravityPull")]
    public bool ExplodeAfterGravityPull = false;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("endSound")]
    public SoundSpecifier? EndSound = new SoundPathSpecifier("/Audio/Effects/Grenades/supermatter_end.ogg");

    public bool IsSecondStageBegan = false;

    public bool IsSecondStageEnded = false;

}
