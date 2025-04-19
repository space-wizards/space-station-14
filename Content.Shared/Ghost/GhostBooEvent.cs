namespace Content.Shared.Ghost;

[ByRefEvent]
public record struct GhostBooEvent(bool Handled = false);
