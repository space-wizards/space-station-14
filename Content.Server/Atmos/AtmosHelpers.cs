using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Map;

#nullable enable

namespace Content.Server.Atmos
{
    public static class AtmosHelpers
    {
        public static TileAtmosphere? GetTileAtmosphere(this GridCoordinates coordinates)
        {
            var gridAtmos = EntitySystem.Get<AtmosphereSystem>().GetGridAtmosphere(coordinates.GridID);

            return gridAtmos?.GetTile(coordinates);
        }

        public static TileAtmosphere? GetTileAtmosphere(this MapIndices indices, GridId gridId)
        {
            var gridAtmos = EntitySystem.Get<AtmosphereSystem>().GetGridAtmosphere(gridId);

            return gridAtmos?.GetTile(indices);
        }
    }
}
