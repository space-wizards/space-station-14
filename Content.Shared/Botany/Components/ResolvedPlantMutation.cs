using Content.Shared.EntityEffects;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Mutation values after overrides have been applied.
/// Stored on plants to track mutations that have occurred.
/// </summary>
[DataRecord]
public readonly partial record struct ResolvedPlantMutation(
    string Name,
    LocId? Description,
    EntityEffect Effect,
    bool AppliesToProduce,
    bool AppliesToPlant,
    bool Persists);
