namespace Content.Shared.Body.Part;

[ByRefEvent]
public readonly record struct BodyPartAddedEvent(string Slot, BodyPartComponent Part);

[ByRefEvent]
public readonly record struct BodyPartRemovedEvent(string Slot, BodyPartComponent Part);
