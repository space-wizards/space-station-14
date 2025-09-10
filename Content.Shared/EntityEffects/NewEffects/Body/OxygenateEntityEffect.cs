namespace Content.Shared.EntityEffects.NewEffects.Body;

/// <remarks>
/// Both Component and System are serverside.
/// </remarks>
public sealed partial class Oxygenate : EntityEffectBase<Oxygenate>
{
    [DataField]
    public float Factor = 1f;
}
