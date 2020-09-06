#nullable enable
using System.Diagnostics.CodeAnalysis;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Atmos
{
    public static class AtmosHelpers
    {
        public static TileAtmosphere? GetTileAtmosphere(this EntityCoordinates coordinates, IEntityManager? entityManager = null)
        {
            entityManager ??= IoCManager.Resolve<IEntityManager>();

            var gridAtmos = EntitySystem.Get<AtmosphereSystem>().GetGridAtmosphere(coordinates.GetGridId(entityManager));

            return gridAtmos?.GetTile(coordinates);
        }

        public static GasMixture? GetTileAir(this EntityCoordinates coordinates)
        {
            return coordinates.GetTileAtmosphere()?.Air;
        }

        public static bool TryGetTileAtmosphere(this EntityCoordinates coordinates, [MaybeNullWhen(false)] out TileAtmosphere atmosphere)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            return !Equals(atmosphere = coordinates.GetTileAtmosphere()!, default);
        }

        public static bool TryGetTileAir(this EntityCoordinates coordinates, [MaybeNullWhen(false)] out GasMixture air)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            return !Equals(air = coordinates.GetTileAir()!, default);
        }

        public static TileAtmosphere? GetTileAtmosphere(this MapIndices indices, GridId gridId)
        {
            var gridAtmos = EntitySystem.Get<AtmosphereSystem>().GetGridAtmosphere(gridId);

            return gridAtmos?.GetTile(indices);
        }

        public static GasMixture? GetTileAir(this MapIndices indices, GridId gridId)
        {
            return indices.GetTileAtmosphere(gridId)?.Air;
        }

        public static bool TryGetTileAtmosphere(this MapIndices indices, GridId gridId,
            [MaybeNullWhen(false)] out TileAtmosphere atmosphere)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            return !Equals(atmosphere = indices.GetTileAtmosphere(gridId)!, default);
        }

        public static bool TryGetTileAir(this MapIndices indices, GridId gridId, [MaybeNullWhen(false)] out GasMixture air)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            return !Equals(air = indices.GetTileAir(gridId)!, default);
        }
    }
}
