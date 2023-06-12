using Content.Shared.FixedPoint;
using Content.Shared.Medical.Consciousness.Components;

namespace Content.Shared.Medical.Consciousness;

[ByRefEvent]
public record struct ConsciousnessUpdatedEvent(bool IsConscious, FixedPoint2 ConsciousnessDelta);
