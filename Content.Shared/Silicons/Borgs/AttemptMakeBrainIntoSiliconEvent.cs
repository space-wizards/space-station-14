using Content.Shared.Silicons.Borgs.Components;

namespace Content.Shared.Silicons.Borgs;

[ByRefEvent]
public record struct AttemptMakeBrainIntoSiliconEvent(
    EntityUid Brain,
    Entity<MMIComponent> BrainHolder,
    bool Cancelled = false);
