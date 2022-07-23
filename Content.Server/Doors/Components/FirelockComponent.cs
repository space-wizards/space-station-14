using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Doors.Systems;
using Content.Shared.Doors.Components;
using Robust.Server.GameObjects;
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
            var transform = _entMan.GetComponent<TransformComponent>(Owner);

            if (transform.GridUid is not {} gridUid)
                return false;

            var atmosphereSystem = _entMan.EntitySysManager.GetEntitySystem<AtmosphereSystem>();
            var transformSystem = _entMan.EntitySysManager.GetEntitySystem<TransformSystem>();

            var position = transformSystem.GetGridOrMapTilePosition(Owner, transform);

            var minMoles = float.MaxValue;
            var maxMoles = 0f;

            foreach (var adjacent in atmosphereSystem.GetAdjacentTileMixtures(gridUid, position))
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
            var atmosphereSystem = _entMan.EntitySysManager.GetEntitySystem<AtmosphereSystem>();
            var transformSystem = _entMan.EntitySysManager.GetEntitySystem<TransformSystem>();

            var transform = _entMan.GetComponent<TransformComponent>(Owner);
            var position = transformSystem.GetGridOrMapTilePosition(Owner, transform);

            // No grid, no fun.
            if (transform.GridUid is not {} gridUid)
                return false;

            if (atmosphereSystem.GetTileMixture(gridUid, null, position) == null)
                return false;

            if (atmosphereSystem.IsHotspotActive(gridUid, position))
                return true;

            foreach (var adjacent in atmosphereSystem.GetAdjacentTiles(gridUid, position))
            {
                if (atmosphereSystem.IsHotspotActive(gridUid, adjacent))
                    return true;
            }

            return false;
        }
    }
}
