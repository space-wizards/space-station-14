namespace Content.Shared.EntityEffects.Effects.Body;

/// <remarks>
/// Both Component and System are serverside.
/// </remarks>
[DataDefinition]
public sealed partial class Oxygenate : EntityEffectBase<Oxygenate>
{
    [DataField]
    public float Factor = 1f;
}
