using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounds.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Wounds.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(WoundSystem))]
public sealed class WoundComponent : Component
{
    [DataField("healthDamage")] public FixedPoint2 HealthDamage;

    [DataField("integrityDamage")] public FixedPoint2 IntegrityDamage;

    [DataField("healthDamageDealt")] public FixedPoint2 HealthDamageDealt;

    [DataField("integrityDamageDealt")] public FixedPoint2 IntegrityDamageDealt;

    [DataField("overflowDamageDealt")] public FixedPoint2 OverflowDamageDealt;
}
