namespace Content.Shared.Medical.MentalState.Systems;

[ByRefEvent]
public readonly record struct MentalStateChangedEvent;

[ByRefEvent]
public readonly record struct BeginUnconsciousEvent;

[ByRefEvent]
public readonly record struct EndUnconsciousEvent;
