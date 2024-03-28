using Content.Shared.Body.Components;

namespace Content.Shared.Body.Part;

[ByRefEvent]
public readonly record struct BodyPartAddedEvent(string Slot, Entity<BodyPartComponent> Part);

[ByRefEvent]
public readonly record struct BodyPartRemovedEvent(string Slot, Entity<BodyPartComponent> Part);


[ByRefEvent]
public readonly record struct BodyPartAddedToBodyEvent(string Slot, Entity<BodyComponent> Body, Entity<BodyPartComponent> Part);

[ByRefEvent]
public readonly record struct BodyPartRemovedFromBodyEvent(string Slot, Entity<BodyComponent> OldBody, Entity<BodyPartComponent> Part);
