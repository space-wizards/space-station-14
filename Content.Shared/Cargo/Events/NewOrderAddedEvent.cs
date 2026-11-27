using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Events;

[ByRefEvent]
public record struct NewOrderAddedEvent(string Message);
