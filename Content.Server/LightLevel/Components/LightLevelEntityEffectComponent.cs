using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;

namespace Content.Server.LightLevel.Components;

/// <summary>
/// Allows you to activate the specified effects at a certain lighting level.
/// </summary>
[RegisterComponent]
public sealed partial class LightLevelEntityEffectComponent : Component
{
    [DataField]
    public List<LightLevelCondition> Conditions = new();
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
