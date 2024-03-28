using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounding.Components;

namespace Content.Shared.Medical.Wounding.Events;

[ByRefEvent]
public record struct WoundableHealAttemptEvent(Entity<WoundableComponent, HealableComponent> TargetWoundable, bool Canceled = false);

[ByRefEvent]
public record struct WoundableHealthChangedEvent(Entity<WoundableComponent> TargetWoundable, FixedPoint2 HealthDelta);

[ByRefEvent]
public record struct WoundableIntegrityChangedEvent(Entity<WoundableComponent> TargetWoundable, FixedPoint2 HealthDelta);
