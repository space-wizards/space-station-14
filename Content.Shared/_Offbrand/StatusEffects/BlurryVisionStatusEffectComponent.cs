namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(BlurryVisionStatusEffectSystem))]
public sealed partial class BlurryVisionStatusEffectComponent : Component
{
    [DataField(required: true)]
    public float Blur;

    [DataField(required: true)]
    public float CorrectionPower;
}
