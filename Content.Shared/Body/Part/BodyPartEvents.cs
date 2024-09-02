namespace Content.Shared.Body.Part;

[ByRefEvent]
public readonly record struct BodyPartAddedEvent(string Slot, Entity<BodyPartComponent> Part);

[ByRefEvent]
public readonly record struct BodyPartRemovedEvent(string Slot, Entity<BodyPartComponent> Part);
