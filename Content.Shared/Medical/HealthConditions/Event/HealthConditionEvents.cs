using Content.Shared.FixedPoint;
using Content.Shared.Medical.HealthConditions.Components;

namespace Content.Shared.Medical.HealthConditions.Event;

[ByRefEvent]
public record struct HealthConditionAddAttemptEvent(Entity<HealthConditionComponent> PossibleCondition, bool Canceled = false);

[ByRefEvent]
public record struct HealthConditionAddedEvent(Entity<HealthConditionComponent> NewCondition);

[ByRefEvent]
public record struct HealthConditionRemoveAttemptEvent(Entity<HealthConditionComponent> Condition, bool Canceled = false);

[ByRefEvent]
public record struct HealthConditionRemovedEvent(Entity<HealthConditionComponent> Condition);

[ByRefEvent]
public record struct HealthConditionSeverityChangeAttemptEvent(Entity<HealthConditionComponent> TargetCondition, FixedPoint2 SeverityDelta, bool Canceled = false);

[ByRefEvent]
public record struct HealthConditionSeverityChangedEvent(Entity<HealthConditionComponent> TargetCondition, FixedPoint2 SeverityDelta);

[ByRefEvent]
public record struct HealthConditionSeveritySetAttemptEvent(Entity<HealthConditionComponent> TargetCondition, FixedPoint2 NewSeverity, bool Canceled = false);

[ByRefEvent]
public record struct HealthConditionSeveritySetEvent(Entity<HealthConditionComponent> TargetCondition, FixedPoint2 OldSeverity);
