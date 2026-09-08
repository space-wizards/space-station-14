using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedAtmosphereSystem
{
    /// <summary>
    /// Merges a given <see cref="GasMixture"/> into this entity's containing <see cref="GasMixture"/>.
    /// </summary>
    /// <param name="entity">Entity who's containing <see cref="GasMixture"/>
    /// we're merging a given <see cref="GasMixture"/> into.</param>
    /// <param name="mixture">The gas <see cref="GasMixture"/>
    /// we're merging into the containing <see cref="GasMixture"/></param>
    /// <param name="ignoreExposed">Whether we should ignore non-tile <see cref="GasMixture"/>s.</param>
    /// <param name="excite">Whether we should excite the gas upon merging.</param>
    [PublicAPI]
    public virtual void MergeContainingMixture(Entity<TransformComponent?> entity, GasMixture mixture, bool ignoreExposed = false, bool excite = false)
    {
        // Handled by server
    }

    /// <summary>
    /// Merges a given gas <see cref="GasMixture"/> into this entity's tile <see cref="GasMixture"/>.
    /// </summary>
    /// <param name="entity">Entity who's containing <see cref="GasMixture"/>
    /// we're merging a given <see cref="GasMixture"/> into.</param>
    /// <param name="mixture">The gas <see cref="GasMixture"/>
    /// we're merging into the containing <see cref="GasMixture"/>.</param>
    /// <param name="excite">Whether we should excite the gas upon merging.</param>
    [PublicAPI]
    public virtual void MergeTileMixture(Entity<TransformComponent?> entity, GasMixture mixture, bool excite = false)
    {
        // Handled by server
    }

    /// <summary>
    /// Adjusts a given gas in this entity's containing <see cref="GasMixture"/>.
    /// </summary>
    /// <param name="entity">Entity who's containing <see cref="GasMixture"/>
    /// we're merging a given <see cref="GasMixture"/> into.</param>
    /// <param name="gas">The gas in our given <see cref="GasMixture"/> we're adjusting the mols of.</param>
    /// <param name="mols">The amount of mols we're adjusting the gas by.</param>
    /// <param name="ignoreExposed">Whether we should ignore non-tile <see cref="GasMixture"/>s.</param>
    /// <param name="excite">Whether we should excite the gas upon merging.</param>
    [PublicAPI]
    public virtual void AdjustContainingMixture(Entity<TransformComponent?> entity, Gas gas, float mols, bool ignoreExposed = false, bool excite = false)
    {
        // Handled by server
    }

    /// <summary>
    /// Adjusts a given gas in this entity's tile <see cref="GasMixture"/>.
    /// </summary>
    /// <param name="entity">Entity who's containing <see cref="GasMixture"/>
    /// we're merging a given <see cref="GasMixture"/> into.</param>
    /// <param name="gas">The gas in our given <see cref="GasMixture"/> we're adjusting the mols of.</param>
    /// <param name="mols">The amount of mols we're adjusting the gas by.</param>
    /// <param name="excite">Whether we should excite the gas upon merging.</param>
    [PublicAPI]
    public virtual void AdjustTileMixture(Entity<TransformComponent?> entity, Gas gas, float mols, bool excite = false)
    {
        // Handled by server
    }

    /// <summary>
    /// Tries to get the <see cref="GasMixture"/> of a containing entity (ex. lockers and cryopods),
    /// does not return tile <see cref="GasMixture"/>s.
    /// </summary>
    /// <param name="entity">Exposed entity that is in some <see cref="GasMixture"/>.</param>
    /// <param name="mixture">The found gas <see cref="GasMixture"/>.</param>
    /// <returns>Returns true if this entity is in an exposed <see cref="GasMixture"/>, false otherwise.</returns>
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

    /// <summary>
    /// Gets the potential energy from overpressure between two gas mixtures.
    /// </summary>
    /// <returns>
    /// Returns the potential energy of the overpressure in Joules.
    /// Value will be positive if the potential energy is outward (mix1 -> mix2)
    /// Value will be negative if potential energy is inward (mix2 -> mix1)
    /// </returns>
    [PublicAPI]
    public float GetOverPressure(GasMixture mix1, GasMixture? mix2 = null)
    {
        return (mix1.Pressure - (mix2?.Pressure ?? 0)) * mix1.Volume;
    }

}
