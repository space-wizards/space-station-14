using Content.Shared.Atmos.Components;
using JetBrains.Annotations;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedAtmosphereSystem
{
    /// <summary>
    /// Gets the <see cref="GasMixture"/> that an entity is contained within.
    /// </summary>
    /// <param name="ent">The entity to get the mixture for.</param>
    /// <param name="ignoreExposed">If true, will ignore mixtures that the entity is contained in
    /// (ex. lockers and cryopods) and just get the tile mixture.</param>
    /// <param name="excite">If true, will mark the tile as active for atmosphere processing.</param>
    /// <returns>A <see cref="GasMixture"/> if one could be found, null otherwise.</returns>
    [PublicAPI]
    public GasMixture? GetContainingMixture(Entity<TransformComponent?> ent, bool ignoreExposed = false, bool excite = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return null;

        return GetContainingMixture(ent, ent.Comp.GridUid, ent.Comp.MapUid, ignoreExposed, excite);
    }

    /// <summary>
    /// Gets the <see cref="GasMixture"/> that an entity is contained within.
    /// </summary>
    /// <param name="ent">The entity to get the mixture for.</param>
    /// <param name="grid">The grid that the entity may be on.</param>
    /// <param name="map">The map that the entity may be on.</param>
    /// <param name="ignoreExposed">If true, will ignore mixtures that the entity is contained in
    /// (ex. lockers and cryopods) and just get the tile mixture.</param>
    /// <param name="excite">If true, will mark the tile as active for atmosphere processing.</param>
    /// <returns>A <see cref="GasMixture"/> if one could be found, null otherwise.</returns>
    [PublicAPI]
    public GasMixture? GetContainingMixture(
        Entity<TransformComponent?> ent,
        Entity<GridAtmosphereComponent?, GasTileOverlayComponent?>? grid,
        Entity<MapAtmosphereComponent?>? map,
        bool ignoreExposed = false,
        bool excite = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return null;

        if (!ignoreExposed && !ent.Comp.Anchored)
        {
            // Used for things like disposals/cryo to change which air people are exposed to.
            var ev = new AtmosExposedGetAirEvent((ent, ent.Comp), excite);
            RaiseLocalEvent(ent, ref ev);
            if (ev.Handled)
                return ev.Gas;

            // TODO ATMOS: recursively iterate up through parents
            // This really needs recursive InContainer metadata flag for performance
            // And ideally some fast way to get the innermost airtight container.
        }

        var position = XformSystem.GetGridTilePositionOrDefault((ent, ent.Comp));
        return GetTileMixture(grid, map, position, excite);
    }

    /// <summary>
    /// Gets the gas mixture for a specific tile that an entity is on.
    /// </summary>
    /// <param name="entity">The entity to get the tile mixture for.</param>
    /// <param name="excite">Whether to mark the tile as active for atmosphere processing.</param>
    /// <returns>A <see cref="GasMixture"/> if one could be found, null otherwise.</returns>
    /// <remarks>This does not return the <see cref="GasMixture"/> that the entity
    /// may be contained in, ex. if the entity is currently in a locker/crate with its own
    /// <see cref="GasMixture"/>.</remarks>
    [PublicAPI]
    public GasMixture? GetTileMixture(Entity<TransformComponent?> entity, bool excite = false)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return null;

        var indices = XformSystem.GetGridTilePositionOrDefault(entity);
        return GetTileMixture(entity.Comp.GridUid, entity.Comp.MapUid, indices, excite);
    }

    /// <summary>
    /// Gets the gas mixture for a specific tile on a grid or map.
    /// </summary>
    /// <param name="grid">The grid to get the mixture from.</param>
    /// <param name="map">The map to get the mixture from.</param>
    /// <param name="gridTile">The tile to get the mixture from.</param>
    /// <param name="excite">Whether to mark the tile as active for atmosphere processing.</param>
    /// <returns>>A <see cref="GasMixture"/> if one could be found, null otherwise.</returns>
    [PublicAPI]
    public virtual GasMixture? GetTileMixture(
        Entity<GridAtmosphereComponent?, GasTileOverlayComponent?>? grid,
        Entity<MapAtmosphereComponent?>? map,
        Vector2i gridTile,
        bool excite = false)
    {
        return GasMixture.SpaceGas;
    }
}
