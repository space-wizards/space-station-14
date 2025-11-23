using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent, NetworkedComponent]
[Access(typeof(MaximumDamageSystem))]
public sealed partial class MaximumDamageComponent : Component
{
    /// <summary>
    /// The maximum damages that can be acquired
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<DamageTypePrototype>, (FixedPoint2 Base, FixedPoint2 Factor)> Damage = default!;
}
