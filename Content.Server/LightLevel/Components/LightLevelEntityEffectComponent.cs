using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.LightLevel.Components;

/// <summary>
/// Allows you to activate the specified effects at a certain lighting level.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class LightLevelEntityEffectComponent : Component
{
    [DataField]
    public List<LightLevelCondition> Conditions = new();

    /// <summary>
    /// Delay between effects applying in seconds
    /// </summary>
    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(0.1);

    /// <summary>
    /// Next time to apply the effects
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables, AutoPausedField]
    public TimeSpan NextEntityEffect;
}

[DataDefinition]
public partial struct LightLevelCondition()
{
    /// <summary>
    /// Minimum lighting level for the effect
    /// </summary>
    [DataField]
    public float MinLight;

    /// <summary>
    /// Maximum lighting level for the effect
    /// </summary>
    [DataField]
    public float MaxLight;

    /// <summary>
    /// Applied effects
    /// </summary>
    [DataField]
    public EntityEffect[] Effects = default!;

    /// <summary>
    /// The scale of the effects
    /// </summary>
    [DataField]
    public FixedPoint2 Scale = 1;
}
