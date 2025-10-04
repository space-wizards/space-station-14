using Content.Shared.FixedPoint;

namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(StrainStatusEffectSystem))]
public sealed partial class StrainStatusEffectComponent : Component
{
    [DataField(required: true)]
    public FixedPoint2 Delta;
}
