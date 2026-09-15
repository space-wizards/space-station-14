namespace Content.Shared.EntityEffects.Effects;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ScaleEyeZoom : EntityEffectBase<ScaleEyeZoom>
{
    /// <summary>
    /// Multiplier for the current target zoom. Values below one zoom in; must be positive and finite.
    /// </summary>
    [DataField(required: true)]
    public float Factor;
}
