using Content.Shared.Chemistry.Components;

namespace Content.Shared.Chemistry.Reaction;

[RegisterComponent]
public sealed class ReactionMixerComponent : Component
{
    /// <summary>
    ///     A list of IDs for categories of reactions that can be mixed (i.e. HOLY for a bible, DRINK for a spoon)
    /// </summary>
    [ViewVariables]
    [DataField("reactionTypes")]
    public List<string> ReactionTypes = default!;
}

[ByRefEvent]
public record struct MixingAttemptEvent(EntityUid Mixed, bool Cancelled = false);

public readonly record struct AfterMixingEvent(EntityUid Mixed, EntityUid Mixer);

[ByRefEvent]
public record struct GetMixableSolutionAttemptEvent(EntityUid Mixed, Solution? MixedSolution = null);
