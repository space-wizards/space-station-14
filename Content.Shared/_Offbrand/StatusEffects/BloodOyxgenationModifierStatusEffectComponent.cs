using Content.Shared.FixedPoint;

namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(BloodOxygenationModifierStatusEffectSystem))]
public sealed partial class BloodOxygenationModifierStatusEffectComponent : Component
{
    /// <summary>
    /// The minimum lung oxygenation this status effect guarantees
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MinimumOxygenation;
}
