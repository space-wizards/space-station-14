using Content.Server.Singularity.Components;
using Content.Shared.Singularity.Components;
using Robust.Shared.Audio;

namespace Content.Server.Explosion.Components;

/// <summary>
/// This component is controlling process of exploding of supermatter grenade.
/// </summary>
[RegisterComponent]
public sealed class SupermatterGrenadeComponent : Component
{
    /// <summary>
    /// true => After grenade pull things in heap it will explode. Require ExplosiveComponent.
    /// false => Grenade will just pull things in heap and delete it self.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("explodeAfterGravityPull")]
    public bool ExplodeAfterGravityPull = false;

    /// <summary>
    /// Time after begining of gravity pull process that will pass and grenade will explode.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("timeTillExplosion")]
    public float TimeTillExplosion = 0f;

    /// <summary>
    /// GravityPullStart is event when grenade enables GravityWell. 
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("gravityPullStartSound")]
    public SoundSpecifier? GravityPullStartSound = new SoundPathSpecifier("/Audio/Effects/Grenades/Supermatter_Start.ogg");

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("gravityPullStartSoundVolume")]
    public float GravityPullStartSoundVolume = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("gravityPullLoopSoundOffset")]
    public float GravityPullLoopSoundOffset = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("gravityPullEndSound")]
    public SoundSpecifier? GravityPullEndSound = new SoundPathSpecifier("/Audio/Effects/Grenades/Supermatter_End.ogg");

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("gravityPullEndSoundVolume")]
    public float GravityPullEndSoundVolume = 5f;

    /// <summary>
    /// true => After activation by timer grenade will be immovable.
    /// false => Grenade will just pull things in heap  
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("anchorOnGravityPull")]
    public bool AnchorOnGravityPull = true;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsGravityPulling = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsGravitySoundBegan = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsExploded = false;


    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan ExplosionWillOccurIn = TimeSpan.Zero;
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan GravityPullWillOccurIn = TimeSpan.Zero;

    #region GravityWell
    /// <summary>
    /// This variable is getting data from attached GravityWell component on startup.
    /// And then used to turn on/off GravityWell.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("baseRadialAcceleration")]
    public float BaseRadialAcceleration = 10f;

    public GravityWellComponent? GravityWell;
    #endregion

    #region SingularityDistortion
    /// <summary>
    /// This variable is getting data from attached GravityWell component on startup.
    /// And then used to turn on/off effect of SingularityDistortion.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("distortionIntensity")]
    public float DistortionIntensity = 1;

    public SingularityDistortionComponent? Distortion;
    #endregion
}
