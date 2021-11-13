using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Player;

namespace Content.Server.Atmos.EntitySystems
{
    public partial class AtmosphereSystem
    {
        private int _spaceWindSoundCooldown = 0;

        private void HighPressureMovements(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile)
        {
            // TODO ATMOS finish this

            if(tile.PressureDifference > 15)
            {
                if(_spaceWindSoundCooldown == 0)
                {
                    var coordinates = tile.GridIndices.ToEntityCoordinates(tile.GridIndex, _mapManager);
                    if(!string.IsNullOrEmpty(SpaceWindSound))
                        SoundSystem.Play(Filter.Pvs(coordinates), SpaceWindSound, coordinates,
                            AudioHelpers.WithVariation(0.125f).WithVolume(MathHelper.Clamp(tile.PressureDifference / 10, 10, 100)));
                }
            }

            foreach (var entity in _gridtileLookupSystem.GetEntitiesIntersecting(tile.GridIndex, tile.GridIndices))
            {
                if (!entity.TryGetComponent(out IPhysBody? physics)
                    || !entity.IsMovedByPressure(out var pressure)
                    || entity.IsInContainer())
                    continue;

                var pressureMovements = physics.Owner.EnsureComponent<MovedByPressureComponent>();
                if (pressure.LastHighPressureMovementAirCycle < gridAtmosphere.UpdateCounter)
                {
                    pressureMovements.ExperiencePressureDifference(gridAtmosphere.UpdateCounter, tile.PressureDifference, tile.PressureDirection, 0, tile.PressureSpecificTarget?.GridIndices.ToEntityCoordinates(tile.GridIndex, _mapManager) ?? EntityCoordinates.Invalid);
                }

            }

            if (tile.PressureDifference > 100)
            {
                // TODO ATMOS Do space wind graphics here!
            }

            _spaceWindSoundCooldown++;
            if (_spaceWindSoundCooldown > 75)
                _spaceWindSoundCooldown = 0;
        }

        private void ConsiderPressureDifference(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, TileAtmosphere other, float difference)
        {
            gridAtmosphere.HighPressureDelta.Add(tile);
            if (difference > tile.PressureDifference)
            {
                tile.PressureDifference = difference;
                tile.PressureDirection = ((Vector2i)(tile.GridIndices - other.GridIndices)).GetDir().ToAtmosDirection();
            }
        }
    }
}
