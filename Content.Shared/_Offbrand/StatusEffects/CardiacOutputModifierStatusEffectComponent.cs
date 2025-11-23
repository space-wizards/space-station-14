namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(CardiacOutputModifierStatusEffectSystem))]
public sealed partial class CardiacOutputModifierStatusEffectComponent : Component
{
    /// <summary>
    /// The minimum cardiac output this status effect guarantees
    /// </summary>
    [DataField(required: true)]
    public float Output;
}
