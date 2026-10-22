using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.Body.Events;

/// <summary>
/// Event called by <see cref="Content.Server.Body.Systems.MetabolizerSystem"/> to get a list of
/// blood like reagents for metabolism to skip.
/// </summary>
[ByRefEvent]
public readonly record struct MetabolismExclusionEvent()
{
    public readonly List<ReagentId> Reagents = [];
}
