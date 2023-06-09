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
    [DataField("addComponentsOffset")]
    public float AddComponentsOffset = 0f;
    /// <summary>
    /// This list of components that will be added on second trigger.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("secondStageComponents")]
    public ComponentRegistry SecondStageComponents = new();
    /// <summary>
    /// Turn on ambience components in second stage.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("isUsingAmbience")]
    public bool IsUsingAmbience = false;
    #region PrivateFields
    [DataField("timeOfComponentsAddition", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan TimeOfComponentsAddition = TimeSpan.Zero;
    [DataField("timeOfNextTrigger", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan TimeOfNextTrigger = TimeSpan.Zero;
    [DataField("isSecondStageActionsBegan")]
    public bool IsSecondStageActionsBegan = false;
    [DataField("isSecondStageBegan")]
    public bool IsSecondStageBegan = false;
    [DataField("isSecondStageEnded")]
    public bool IsSecondStageEnded = false;
    #endregion
}
