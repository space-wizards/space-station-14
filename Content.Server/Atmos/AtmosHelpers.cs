#nullable enable
using System.Diagnostics.CodeAnalysis;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Atmos
{
    public static class AtmosHelpers
    {
        public static TileAtmosphere? GetTileAtmosphere(this EntityCoordinates coordinates)
        {
            return EntitySystem.Get<AtmosphereSystem>().GetGridAtmosphere(coordinates).GetTile(coordinates);
        }

        public static GasMixture? GetTileAir(this EntityCoordinates coordinates, IEntityManager? entityManager = null)
        {
            return coordinates.GetTileAtmosphere()?.Air;
        }

        public static bool TryGetTileAtmosphere(this EntityCoordinates coordinates, [NotNullWhen(true)] out TileAtmosphere? atmosphere)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            return !Equals(atmosphere = coordinates.GetTileAtmosphere(), default);
        }

        public static bool TryGetTileAir(this EntityCoordinates coordinates, [NotNullWhen(true)] out GasMixture? air, IEntityManager? entityManager = null)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            return !Equals(air = coordinates.GetTileAir(entityManager), default);
        }

        public static bool IsTileAirProbablySafe(this EntityCoordinates coordinates)
        {
            // Note that oxygen mix isn't checked, but survival boxes make that not necessary.
            var air = coordinates.GetTileAir();
            if (air == null)
                return false;
            if (air.Pressure <= Atmospherics.WarningLowPressure)
                return false;
            if (air.Pressure >= Atmospherics.WarningHighPressure)
                return false;
            if (air.Temperature <= 260)
                return false;
            if (air.Temperature >= 360)
                return false;
            return true;
        }

        public static bool InvalidateTileAir(this EntityCoordinates coordinates)
        {
            if (!coordinates.TryGetTileAtmosphere(out var tileAtmos))
            {
                return false;
            }

            tileAtmos.Invalidate();
            return true;
        }
    }
}
