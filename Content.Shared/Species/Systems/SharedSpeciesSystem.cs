using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Species.Systems;

public sealed class SharedSpeciesSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    /// <summary>
    /// Returns the list of requirements for a role, or null. May be altered by requirement overrides.
    /// </summary>
    public HashSet<SpeciesRestriction>? GetSpeciesRestrictions(SpeciesPrototype species)
    {
        return species.Restrictions;
    }

    /// <inheritdoc cref="GetSpeciesRestrictions(SpeciesPrototype)"/>
    public HashSet<SpeciesRestriction>? GetSpeciesRestrictions(ProtoId<SpeciesPrototype> speciesId)
    {
        return _prototypes.TryIndex(speciesId, out var job) ? GetSpeciesRestrictions(job) : null;
    }
}
