using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.EntitySystems
{
    public partial class AtmosphereSystem
    {
        private const int SpaceWindSoundCooldownCycles = 75;

        private int _spaceWindSoundCooldown = 0;

        [ViewVariables(VVAccess.ReadWrite)]
        public string? SpaceWindSound { get; private set; } = "/Audio/Effects/space_wind.ogg";

        private void HighPressureMovements(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, EntityQuery<PhysicsComponent> bodies, EntityQuery<TransformComponent> xforms, EntityQuery<MovedByPressureComponent> pressureQuery)
        {
            // TODO ATMOS finish this

            // Don't play the space wind sound on tiles that are on fire...
            if(tile.PressureDifference > 15 && !tile.Hotspot.Valid)
            {
                if(_spaceWindSoundCooldown == 0 && !string.IsNullOrEmpty(SpaceWindSound))
                {
                    var coordinates = tile.GridIndices.ToEntityCoordinates(tile.GridIndex, _mapManager);
                    SoundSystem.Play(Filter.Pvs(coordinates), SpaceWindSound, coordinates,
                        AudioHelpers.WithVariation(0.125f).WithVolume(MathHelper.Clamp(tile.PressureDifference / 10, 10, 100)));
                }
            }

            foreach (var entity in _lookup.GetEntitiesIntersecting(tile.GridIndex, tile.GridIndices))
            {
                // Ideally containers would have their own EntityQuery internally or something given recursively it may need to slam GetComp<T> anyway.
                if (!bodies.HasComponent(entity)
                    || !pressureQuery.TryGetComponent(entity, out var pressure) || !pressure.Enabled
                    || _containers.IsEntityInContainer(entity, xforms.GetComponent(entity)))
                    continue;

                var pressureMovements = EnsureComp<MovedByPressureComponent>(entity);
                if (pressure.LastHighPressureMovementAirCycle < gridAtmosphere.UpdateCounter)
                {
                    pressureMovements.ExperiencePressureDifference(gridAtmosphere.UpdateCounter, tile.PressureDifference, tile.PressureDirection, 0, tile.PressureSpecificTarget?.GridIndices.ToEntityCoordinates(tile.GridIndex, _mapManager) ?? EntityCoordinates.Invalid);
                }

            }

            if (tile.PressureDifference > 100)
            {
                // TODO ATMOS Do space wind graphics here!
            }

            if (_spaceWindSoundCooldown++ > SpaceWindSoundCooldownCycles)
                _spaceWindSoundCooldown = 0;
        }

        private void ConsiderPressureDifference(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, TileAtmosphere other, float difference)
        {
            gridAtmosphere.HighPressureDelta.Add(tile);
            if (difference > tile.PressureDifference)
            {
                tile.PressureDifference = difference;
                tile.PressureDirection = (tile.GridIndices - other.GridIndices).GetDir().ToAtmosDirection();
            }
        }
    }
}
