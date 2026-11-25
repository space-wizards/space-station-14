namespace Content.Shared.EntityEffects.Effects.Body;

/// <summary>
/// See serverside system.
/// </summary>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Oxygenate : EntityEffectBase<Oxygenate>
{
    /// <summary>
    /// Factor of oxygenation per metabolized quantity. Lungs metabolize at about 50u per tick so we need an equal multiplier to cancel that out!
    /// </summary>
    [DataField]
    public float Factor = 1f;
}
