using Content.Shared.FixedPoint;
using Content.Shared.Medical.Consciousness.Components;

namespace Content.Shared.Medical.Consciousness;

[ByRefEvent]
public record struct UpdateConsciousnessClampEvent(ConsciousnessComponent Component, FixedPoint2 Clamp, bool Canceled = false);

[ByRefEvent]
public record struct UpdateConsciousnessDamageEvent(ConsciousnessComponent Component, FixedPoint2 Damage, bool Canceled = false);

[ByRefEvent]
public record struct UpdateConsciousnessModifierEvent(ConsciousnessComponent Component, FixedPoint2 Modifier, bool Canceled = false);

[ByRefEvent]
public record struct UpdateConsciousnessThresholdEvent(ConsciousnessComponent Component, FixedPoint2 Threshold, bool Canceled = false);



[ByRefEvent]
public record struct ConsciousnessUpdatedEvent(bool IsConscious, FixedPoint2 Consciousness);
