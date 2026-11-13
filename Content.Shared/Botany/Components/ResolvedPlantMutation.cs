using Content.Shared.EntityEffects;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Mutation values after overrides have been applied.
/// Used when applying a mutation to a plant.
/// </summary>
[DataRecord]
public readonly partial record struct ResolvedPlantMutation(
    string Name,
    LocId? Description,
    EntityEffect Effect);
