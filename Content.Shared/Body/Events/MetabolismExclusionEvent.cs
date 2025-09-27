using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.Body.Events;

/// <summary>
/// Event called by <see cref="Content.Server.Body.Systems.MetabolizerSystem"/> to get a list excluded of
/// reagents that Bloodstream deems as blood reagents.
/// </summary>
[ByRefEvent]
public readonly record struct MetabolismExclusionEvent(List<ReagentQuantity> ReagentList);