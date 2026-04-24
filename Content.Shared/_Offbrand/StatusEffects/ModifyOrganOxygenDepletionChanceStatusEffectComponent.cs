using Content.Shared.FixedPoint;

namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(ModifyOrganOxygenDepletionChanceStatusEffectSystem))]
public sealed partial class ModifyOrganOxygenDepletionChanceStatusEffectComponent : Component
{
    /// <summary>
    /// Thresholds for how much to modify the chance of depleting oxygen. Lowest selected.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, double> OxygenationModifierThresholds;
}
