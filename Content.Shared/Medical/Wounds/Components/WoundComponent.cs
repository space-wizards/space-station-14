using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounds.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Wounds.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(WoundSystem))]
public sealed class WoundComponent : Component
{
    [DataField("healthCapDamage")] public FixedPoint2 HealthCapDamage;

    [DataField("integrityDamage")] public FixedPoint2 IntegrityDamage;

    [DataField("severityPercentage")] public FixedPoint2 SeverityPercentage = 1.0;
}
