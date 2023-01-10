using Content.Shared.FixedPoint;

namespace Content.Shared.Medical.Consciousness;

[ByRefEvent]
public record struct UpdateBaseConsciousnessEvent(FixedPoint2 Base);

[ByRefEvent]
public record struct UpdateConsciousnessModifierEvent(FixedPoint2 Modifier);

[ByRefEvent]
public record struct UpdateConsciousnessOffsetEvent(FixedPoint2 Offset);

[ByRefEvent]
public record struct UpdateConsciousnessCapEvent(FixedPoint2 Cap);

[ByRefEvent]
public record struct ConsciousnessUpdateEvent(bool IsConscious, FixedPoint2 Consciousness);
