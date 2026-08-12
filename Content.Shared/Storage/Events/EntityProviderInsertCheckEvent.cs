namespace Content.Shared.Storage.Events;

[ByRefEvent]
public record struct EntityProviderInsertCheckEvent(string? FailureMessage = null);
