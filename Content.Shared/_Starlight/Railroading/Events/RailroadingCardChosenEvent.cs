namespace Content.Shared._Starlight.Railroading.Events;

[ByRefEvent]
public record struct RailroadingCardChosenEvent(
    Entity<RailroadableComponent> Subject
)
{ }

[ByRefEvent]
public record struct RailroadingCardCompletionQueryEvent()
{ 
    public bool? IsCompleted { get; set; } = null;
}

[ByRefEvent]
public record struct RailroadingCardCompletedEvent(
    Entity<RailroadableComponent> Subject
)
{
}

[ByRefEvent]
public record struct RailroadingCardFailedEvent(
    Entity<RailroadableComponent> Subject
)
{ }