#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos;
using Content.Shared.Atmos;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.Components.Atmos
{
    public static class AtmosphericHelpers
    {
        public static TileAtmosphere? GetTileAtmosphere(this GridCoordinates coordinates)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();

            if (!mapManager.TryGetGrid(coordinates.GridID, out var mapGrid))
            {
                return null;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!entityManager.TryGetEntity(mapGrid.GridEntityId, out var grid))
            {
                return null;
            }

            if (!grid.TryGetComponent(out IGridAtmosphereComponent atmosphere))
            {
                return null;
            }

            return atmosphere.GetTile(coordinates);
        }

        public static bool TryGetTileAtmosphere(this GridCoordinates coordinates, [NotNullWhen(true)] out TileAtmosphere tileAtmosphere)
        {
            tileAtmosphere = default!;
            var mapManager = IoCManager.Resolve<IMapManager>();

            if (!mapManager.TryGetGrid(coordinates.GridID, out var mapGrid))
            {
                return false;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!entityManager.TryGetEntity(mapGrid.GridEntityId, out var grid))
            {
                return false;
            }

            if (!grid.TryGetComponent(out IGridAtmosphereComponent atmosphere))
            {
                return false;
            }

            return (tileAtmosphere = atmosphere.GetTile(coordinates)) != default;
        }

        public static Dictionary<Gas, T> GasStructDictionary<T>() where T : struct
        {
            var dict = new Dictionary<Gas, T>();

            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                dict[(Gas) i] = default;
            }

            return dict;
        }

        public static Dictionary<Gas, T?> GasClassDictionary<T>() where T : class
        {
            var dict = new Dictionary<Gas, T?>();

            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                dict[(Gas) i] = default;
            }

            return dict;
        }
    }
}
