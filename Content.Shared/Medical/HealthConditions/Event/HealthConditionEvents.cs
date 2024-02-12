using Content.Shared.FixedPoint;
using Content.Shared.Medical.HealthConditions.Components;

namespace Content.Shared.Medical.HealthConditions.Event;

[ByRefEvent]
public record struct HealthConditionAddAttemptEvent(Entity<HealthConditionComponent> PossibleAffliction, bool Canceled = false);

[ByRefEvent]
public record struct HealthConditionAddedEvent(Entity<HealthConditionComponent> NewAffliction);

[ByRefEvent]
public record struct HealthConditionRemoveAttemptEvent(Entity<HealthConditionComponent> Affliction, bool Canceled = false);

[ByRefEvent]
public record struct HealthConditionRemovedEvent(Entity<HealthConditionComponent> Affliction);

[ByRefEvent]
public record struct HealthConditionSeverityChangeAttemptEvent(Entity<HealthConditionComponent> TargetAffliction, FixedPoint2 SeverityDelta, bool Canceled = false);

[ByRefEvent]
public record struct HealthConditionSeverityChangedEvent(Entity<HealthConditionComponent> TargetAffliction, FixedPoint2 SeverityDelta);

