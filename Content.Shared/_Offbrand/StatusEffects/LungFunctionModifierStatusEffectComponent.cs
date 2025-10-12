namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(LungFunctionModifierStatusEffectSystem))]
public sealed partial class LungFunctionModifierStatusEffectComponent : Component
{
    /// <summary>
    /// The minimum lung function this status effect guarantees
    /// </summary>
    [DataField(required: true)]
    public float Function;
}
