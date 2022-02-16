using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Doors.Systems;
using Content.Shared.Doors.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Doors.Components
{
    /// <summary>
    /// Companion component to ServerDoorComponent that handles firelock-specific behavior -- primarily prying,
    /// and not being openable on open-hand click.
    /// </summary>
    [RegisterComponent]
    public sealed class FirelockComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        /// <summary>
        /// Pry time modifier to be used when the firelock is currently closed due to fire or pressure.
        /// </summary>
        /// <returns></returns>
        [DataField("lockedPryTimeModifier")]
        public float LockedPryTimeModifier = 1.5f;

        public bool EmergencyPressureStop()
        {
            var doorSys = EntitySystem.Get<DoorSystem>();
            if (_entMan.TryGetComponent<DoorComponent>(Owner, out var door) &&
                door.State == DoorState.Open &&
                doorSys.CanClose(Owner, door))
            {
                doorSys.StartClosing(Owner, door);

                // Door system also sets airtight, but only after a delay. We want it to be immediate.
                if (_entMan.TryGetComponent(Owner, out AirtightComponent? airtight))
                {
                    EntitySystem.Get<AirtightSystem>().SetAirblocked(airtight, true);
                }
                return true;
            }
            return false;
        }

        public bool IsHoldingPressure(float threshold = 20)
        {
            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();

            var minMoles = float.MaxValue;
            var maxMoles = 0f;

            foreach (var adjacent in atmosphereSystem.GetAdjacentTileMixtures(_entMan.GetComponent<TransformComponent>(Owner).Coordinates))
            {
                var moles = adjacent.TotalMoles;
                if (moles < minMoles)
                    minMoles = moles;
                if (moles > maxMoles)
                    maxMoles = moles;
            }

            return (maxMoles - minMoles) > threshold;
        }

        public bool IsHoldingFire()
        {
            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();

            if (!atmosphereSystem.TryGetGridAndTile(_entMan.GetComponent<TransformComponent>(Owner).Coordinates, out var tuple))
                return false;

            if (atmosphereSystem.GetTileMixture(tuple.Value.Grid, tuple.Value.Tile) == null)
                return false;

            if (atmosphereSystem.IsHotspotActive(tuple.Value.Grid, tuple.Value.Tile))
                return true;

            foreach (var adjacent in atmosphereSystem.GetAdjacentTiles(_entMan.GetComponent<TransformComponent>(Owner).Coordinates))
            {
                if (atmosphereSystem.IsHotspotActive(tuple.Value.Grid, adjacent))
                    return true;
            }

            return false;
        }
    }
}
