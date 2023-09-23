using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.Chemistry.Reaction;

[ByRefEvent]
public record struct TransferReagentEvent(ReactionMethod Method, ReagentPrototype Proto, ReagentQuantity ReagentQuantity);
