using Content.Shared.Chasm.Components;

namespace Content.Shared.Chasm.Events;

[ByRefEvent]
public record struct StartChasmFallingEvent(Entity<ChasmComponent> Chasm);
