using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.HealthConditions.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HealthConditionComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Severity = 0;

    public FixedPoint2 SeverityAsMultiplier => Severity / SeverityMax;

    [DataField, AutoNetworkedField]
    public EntityUid ConditionManager = EntityUid.Invalid;

    public const float SeverityMax = 100f;

}
