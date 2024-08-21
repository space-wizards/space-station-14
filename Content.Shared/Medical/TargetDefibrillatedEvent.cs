namespace Content.Shared.Medical;

[ByRefEvent]
public readonly record struct TargetDefibrillatedEvent(EntityUid User, Entity<DefibrillatorComponent> Defibrillator);
