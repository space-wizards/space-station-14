using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Doors;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Doors.Components
{
    /// <summary>
    /// Companion component to ServerDoorComponent that handles firelock-specific behavior -- primarily prying,
    /// and not being openable on open-hand click.
    /// </summary>
    [RegisterComponent]
    public class FirelockComponent : Component
    {
        public override string Name => "Firelock";

        [ComponentDependency]
        public readonly ServerDoorComponent? DoorComponent = null;

        /// <summary>
        /// Pry time modifier to be used when the firelock is currently closed due to fire or pressure.
        /// </summary>
        /// <returns></returns>
        [DataField("lockedPryTimeModifier")]
        public float LockedPryTimeModifier = 1.5f;

        public bool EmergencyPressureStop()
        {
            if (DoorComponent != null && DoorComponent.State == SharedDoorComponent.DoorState.Open && DoorComponent.CanCloseGeneric())
            {
                DoorComponent.Close();
                if (Owner.TryGetComponent(out AirtightComponent? airtight))
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

            foreach (var adjacent in atmosphereSystem.GetAdjacentTileMixtures(Owner.Transform.Coordinates))
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

            if (!atmosphereSystem.TryGetGridAndTile(Owner.Transform.Coordinates, out var tuple))
                return false;

            if (atmosphereSystem.GetTileMixture(tuple.Value.Grid, tuple.Value.Tile) == null)
                return false;

            if (atmosphereSystem.IsHotspotActive(tuple.Value.Grid, tuple.Value.Tile))
                return true;

            foreach (var adjacent in atmosphereSystem.GetAdjacentTiles(Owner.Transform.Coordinates))
            {
                if (atmosphereSystem.IsHotspotActive(tuple.Value.Grid, adjacent))
                    return true;
            }

            return false;
        }
    }
}
