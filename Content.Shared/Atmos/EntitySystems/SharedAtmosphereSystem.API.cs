using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedAtmosphereSystem
{
    /// <summary>
    /// Merges a given gas mixture into this entity's containing mixture.
    /// </summary>
    /// <param name="entity">Entity who's containing mixture we're merging a given mixture into.</param>
    /// <param name="mixture">The gas mixture we're merging into the containing mixture</param>
    /// <param name="ignoreExposed">Whether we should ignore non-tile mixtures.</param>
    /// <param name="excite">Whether we should excite the gas upon merging.</param>
    [PublicAPI]
    public virtual void MergeContainingMixture(Entity<TransformComponent?> entity, GasMixture mixture, bool ignoreExposed = false, bool excite = false)
    {
        // Handled by server
    }

    /// <summary>
    /// Merges a given gas mixture into this entity's tile mixture.
    /// </summary>
    /// <param name="entity">Entity who's containing mixture we're merging a given mixture into.</param>
    /// <param name="mixture">The gas mixture we're merging into the containing mixture</param>
    /// <param name="excite">Whether we should excite the gas upon merging.</param>
    [PublicAPI]
    public virtual void MergeTileMixture(Entity<TransformComponent?> entity, GasMixture mixture, bool excite = false)
    {
        // Handled by server
    }

    /// <summary>
    /// Adjusts a given gas in this entity's containing mixture.
    /// </summary>
    /// <param name="entity">Entity who's containing mixture we're merging a given mixture into.</param>
    /// <param name="gas">The gas in our given mixture we're adjusting the mols of.</param>
    /// <param name="mols">The amount of mols we're adjusting the gas by.</param>
    /// <param name="ignoreExposed">Whether we should ignore non-tile mixtures.</param>
    /// <param name="excite">Whether we should excite the gas upon merging.</param>
    [PublicAPI]
    public virtual void AdjustContainingMixture(Entity<TransformComponent?> entity, Gas gas, float mols, bool ignoreExposed = false, bool excite = false)
    {
        // Handled by server
    }

    /// <summary>
    /// Adjusts a given gas in this entity's tile mixture.
    /// </summary>
    /// <param name="entity">Entity who's containing mixture we're merging a given mixture into.</param>
    /// <param name="gas">The gas in our given mixture we're adjusting the mols of.</param>
    /// <param name="mols">The amount of mols we're adjusting the gas by.</param>
    /// <param name="excite">Whether we should excite the gas upon merging.</param>
    [PublicAPI]
    public virtual void AdjustTileMixture(Entity<TransformComponent?> entity, Gas gas, float mols, bool excite = false)
    {
        // Handled by server
    }

    /// <summary>
    /// Tries to get the mixture of a containing entity (ex. lockers and cryopods), does not return tile mixtures.
    /// </summary>
    /// <param name="entity">Exposed entity that is in some gas mixture.</param>
    /// <param name="mixture">The found gas mixture.</param>
    /// <returns>Returns true if this entity is in an exposed mixture, false otherwise.</returns>
    [PublicAPI]
    public bool TryGetExposedMixture(Entity<TransformComponent?> entity, [NotNullWhen(true)] out GasMixture? mixture)
    {
        mixture = null;
        if (!Resolve(entity, ref entity.Comp) || entity.Comp.Anchored)
            return false;

        // TODO ATMOS: recursively iterate up through parents
        // This really needs recursive InContainer metadata flag for performance
        // And ideally some fast way to get the innermost airtight container.
        var ev = new AtmosExposedGetAirEvent((entity, entity.Comp));
        RaiseLocalEvent(entity, ref ev);
        mixture = ev.Gas;

        return ev.Handled;
    }
}
