using Content.Shared.Chemistry.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Reaction;

[RegisterComponent]
public sealed partial class ReactionMixerComponent : Component
{
    /// <summary>
    ///     A list of IDs for categories of reactions that can be mixed (i.e. HOLY for a bible, DRINK for a spoon)
    /// </summary>
    [ViewVariables]
    [DataField]
    public List<ProtoId<MixingCategoryPrototype>> ReactionTypes = default!;

    /// <summary>
    ///     A string which identifies the string to be sent when successfully mixing a solution
    /// </summary>
    [ViewVariables]
    [DataField]
    public LocId MixMessage = "default-mixing-success";
}

[ByRefEvent]
public record struct MixingAttemptEvent(EntityUid Mixed, bool Cancelled = false);

public readonly record struct AfterMixingEvent(EntityUid Mixed, EntityUid Mixer);

[ByRefEvent]
public record struct GetMixableSolutionAttemptEvent(EntityUid Mixed, Entity<SolutionComponent>? MixedSolution = null);
