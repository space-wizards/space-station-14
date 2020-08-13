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

        public static bool TryGetTileAtmosphere(this GridCoordinates coordinates, [NotNullWhen(true)] out TileAtmosphere atmosphere)
        {
            return (atmosphere = coordinates.GetTileAtmosphere()!) != default;
        }

        public static bool TryGetTileAir(this GridCoordinates coordinates, [NotNullWhen(true)] out GasMixture air)
        {
            return !(air = coordinates.GetTileAir()!).Equals(default);
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
            [NotNullWhen(true)] out TileAtmosphere atmosphere)
        {
            return (atmosphere = indices.GetTileAtmosphere(gridId)!) != default;
        }

        public static bool TryGetTileAir(this MapIndices indices, GridId gridId, [NotNullWhen(true)] out GasMixture air)
        {
            return !(air = indices.GetTileAir(gridId)!).Equals(default);
        }
    }
}
