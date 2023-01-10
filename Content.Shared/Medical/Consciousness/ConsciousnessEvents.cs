using Content.Shared.FixedPoint;
using Content.Shared.Medical.Consciousness.Components;

namespace Content.Shared.Medical.Consciousness;

[ByRefEvent]
public record struct UpdateConsciousnessValuesEvent(ConsciousnessComponent Component);

[ByRefEvent]
public record struct ConsciousnessUpdateEvent(bool IsConscious, FixedPoint2 Consciousness);
