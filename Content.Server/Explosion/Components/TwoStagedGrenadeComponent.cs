using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Explosion.Components;

/// <summary>
/// This component after trigger starts timer to trigger for second time to explode or delete self.
/// 
/// For example this is controlling process of exploding of supermatter grenade,
/// first trigger enables GravityWell, and second makes grenade explode.
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
    /// <summary>
    /// Offset when AmbienceComponent will be enabled after first trigger.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("ambienceSoundOffset")]
    public float AmbienceSoundOffset = 0f;
    /// <summary>
    /// true => After second stage grenade will explode. Require ExplosiveComponent.
    /// false => Grenade will just pull things in heap and delete itself.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("explodeAfterGravityPull")]
    public bool ExplodeAfterGravityPull = false;
    /// <summary>
    /// Sound that plays on the end of second stage
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("endSound")]
    public SoundSpecifier? EndSound = new SoundPathSpecifier("/Audio/Effects/Grenades/supermatter_end.ogg");

    #region PrivateFields
    [DataField("ambienceStartTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan AmbienceStartTime = TimeSpan.Zero;
    [DataField("ambienceStartTime")]
    public TimeSpan TimeOfExplosion = TimeSpan.Zero;
    [DataField("isSecondStageSoundBegan")]
    public bool IsSecondStageSoundBegan = false;
    [DataField("isSecondStageBegan")]
    public bool IsSecondStageBegan = false;
    [DataField("isSecondStageEnded")]
    public bool IsSecondStageEnded = false;
    #endregion
}
