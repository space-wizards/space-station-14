using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent, NetworkedComponent]
[Access(typeof(IntrinsicPainSystem))]
public sealed partial class IntrinsicPainComponent : Component
{
    /// <summary>
    /// Coefficients for damage to pain
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> PainCoefficients;
}
