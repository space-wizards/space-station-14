using Content.Shared.FixedPoint;
using Content.Shared.Medical.Consciousness.Components;

namespace Content.Shared.Medical.Consciousness.Events;


[ByRefEvent]
public record struct ChangeConsciousnessAttemptEvent(Entity<ConsciousnessComponent> TargetConsciousness, FixedPoint2 PossibleDelta, bool Canceled = false);

[ByRefEvent]
public record struct ConsciousnessChangedEvent(Entity<ConsciousnessComponent> TargetConsciousness, FixedPoint2 ConsciousnessDelta);

[ByRefEvent]
public record struct EntityPassOutAttemptEvent(Entity<ConsciousnessComponent> TargetConsciousness, bool Canceled = false);

[ByRefEvent]
public record struct EntityWakeUpAttemptEvent(Entity<ConsciousnessComponent> TargetConsciousness, bool Canceled = false);

[ByRefEvent]
public record struct EntityPassOutEvent(Entity<ConsciousnessComponent> TargetConsciousness);

[ByRefEvent]
public record struct EntityWakeUpEvent(Entity<ConsciousnessComponent> TargetConsciousness);

[ByRefEvent]
public record struct EntityConsciousnessKillAttemptEvent(Entity<ConsciousnessComponent> TargetConsciousness,  bool Canceled = false);

[ByRefEvent]
public record struct EntityConsciousnessKillEvent(Entity<ConsciousnessComponent> TargetConsciousness);
