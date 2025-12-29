namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(VascularToneModifierStatusEffectSystem))]
public sealed partial class VascularToneModifierStatusEffectComponent : Component
{
    /// <summary>
    /// The minimum vascular tone this status effect guarantees
    /// </summary>
    [DataField(required: true)]
    public float Tone;
}
