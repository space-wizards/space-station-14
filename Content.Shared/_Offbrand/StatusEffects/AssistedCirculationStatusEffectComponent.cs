using Content.Shared.FixedPoint;

namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(AssistedCirculationStatusEffectSystem))]
public sealed partial class AssistedCirculationStatusEffectComponent : Component
{
    /// <summary>
    /// How much blood circulation to add
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 Amount;
}
