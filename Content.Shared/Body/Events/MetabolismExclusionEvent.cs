using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.Body.Events;

[ByRefEvent]
public readonly record struct MetabolismExclusionEvent(List<ReagentQuantity> ReagentList);