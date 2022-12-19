namespace Content.Shared.EntityFlags.Systems;

[ByRefEvent]
public readonly record struct EntityFlagAddedEvent(byte FlagGroupId, string Flag);
