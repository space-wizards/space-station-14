using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.HealthConditions.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HealthConditionComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 RawSeverity = 0;

    public FixedPoint2 SeverityAsMultiplier => RawSeverity / SeverityMax;

    [DataField, AutoNetworkedField]
    public EntityUid ConditionManager = EntityUid.Invalid;

    public FixedPoint2 Severity => FixedPoint2.Clamp(RawSeverity, 0, SeverityMax);

    public const float SeverityMax = 100f;

}
