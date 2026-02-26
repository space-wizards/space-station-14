using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent, NetworkedComponent]
[Access(typeof(BrainDamageOnDamageSystem))]
public sealed partial class BrainDamageOnDamageComponent : Component
{
    [DataField(required: true)]
    public List<OrganDamageThresholdSpecifier> Thresholds;
}
