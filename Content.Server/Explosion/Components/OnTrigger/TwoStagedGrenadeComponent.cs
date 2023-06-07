using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Explosion.Components;

/// <summary>
/// This component after trigger starts timer to trigger for second time to add more components and start ambience.
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
    public float ExplosionDelay = 10f;
    /// <summary>
    /// Offset when AmbienceComponent will be enabled after first trigger.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("ambienceSoundOffset")]
    public float AmbienceSoundOffset = 0f;
    /// <summary>
    /// This list of components that will be added on second trigger
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("secondStageComponents")]
    public ComponentRegistry SecondStageComponents = new();

    #region PrivateFields
    [DataField("ambienceStartTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan AmbienceStartTime = TimeSpan.Zero;
    [DataField("timeOfExplosion", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan TimeOfExplosion = TimeSpan.Zero;
    [DataField("isSecondStageSoundBegan")]
    public bool IsSecondStageSoundBegan = false;
    [DataField("isSecondStageBegan")]
    public bool IsSecondStageBegan = false;
    [DataField("isSecondStageEnded")]
    public bool IsSecondStageEnded = false;
    [DataField("isComponentsLoaded")]
    public bool IsComponentsLoaded = false;
    #endregion
}
