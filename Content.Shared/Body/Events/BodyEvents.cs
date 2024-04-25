using Content.Shared.Body.Components;

namespace Content.Shared.Body.Events;


[ByRefEvent]
public readonly record struct BodyInitializedEvent(Entity<BodyComponent> Body);
