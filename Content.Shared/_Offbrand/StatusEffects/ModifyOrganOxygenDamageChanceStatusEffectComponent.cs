using Content.Shared.FixedPoint;

namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(ModifyOrganOxygenDamageChanceStatusEffectSystem))]
public sealed partial class ModifyOrganOxygenDamageChanceStatusEffectComponent : Component
{
    /// <summary>
    /// Thresholds for how much to modify the chance of taking brain damage. Lowest selected.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, double> OxygenationModifierThresholds;
}
