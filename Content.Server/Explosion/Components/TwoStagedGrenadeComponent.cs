using Content.Server.Singularity.Components;
using Content.Shared.Singularity.Components;
using Robust.Shared.Audio;

namespace Content.Server.Explosion.Components;

/// <summary>
/// This component is controlling process of exploding of supermatter grenade.
/// </summary>
[RegisterComponent]
public sealed class TwoStagedGrenadeComponent : Component
{
    /// <summary>
    /// true => After grenade pull things in heap it will explode. Require ExplosiveComponent.
    /// false => Grenade will just pull things in heap and delete it self.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("explodeAfterGravityPull")]
    public bool ExplodeAfterGravityPull = false;

    /// <summary>
    /// Time after first trigger that will pass and grenade will be triggered again.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("explossionDelay")]
    public float ExplossionDelay = 0f;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeOfExplosion = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("ambienceSoundOffset")]
    public float AmbienceSoundOffset = 0f;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan AmbienceStartTime = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("endSound")]
    public SoundSpecifier? EndSound = new SoundPathSpecifier("/Audio/Effects/Grenades/supermatter_end.ogg");

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsSecondStageBegan = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsSecondStageSoundBegan = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsSecondStageEnded = false;
}
