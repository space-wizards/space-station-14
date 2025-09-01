using Content.Shared.Chemistry.Reagent;

namespace Content.Shared._Starlight.Railroading.Events;

[ByRefEvent]
public record struct RailroadingReagentMetabolizedEvent(
    ReagentQuantity Reagent
)
{ }