namespace Content.Shared.EntityFlags.Systems;

[ByRefEvent]
public readonly record struct EntityFlagRemovedEvent(byte FlagGroupId, string Flag);
