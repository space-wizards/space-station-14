#nullable enable
using System.Diagnostics.CodeAnalysis;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Map;

namespace Content.Server.Atmos
{
    public static class AtmosHelpers
    {
        public static TileAtmosphere? GetTileAtmosphere(this GridCoordinates coordinates)
        {
            var gridAtmos = EntitySystem.Get<AtmosphereSystem>().GetGridAtmosphere(coordinates.GridID);

            return gridAtmos?.GetTile(coordinates);
        }

        public static GasMixture? GetTileAir(this GridCoordinates coordinates)
        {
            return coordinates.GetTileAtmosphere()?.Air;
        }

        public static bool TryGetTileAtmosphere(this GridCoordinates coordinates, [MaybeNullWhen(false)] out TileAtmosphere atmosphere)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            return !Equals(atmosphere = coordinates.GetTileAtmosphere()!, default);
        }

        public static bool TryGetTileAir(this GridCoordinates coordinates, [MaybeNullWhen(false)] out GasMixture air)
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
