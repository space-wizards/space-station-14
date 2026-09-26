namespace Content.Shared.CosmicCult;

/// <summary>
///     Event dispatched from shared into server code where something creates another thing that should be associated with the gamerule
/// </summary>
[ByRefEvent]
public record struct CosmicCultAssociateRuleEvent(EntityUid Originator, EntityUid Target);
