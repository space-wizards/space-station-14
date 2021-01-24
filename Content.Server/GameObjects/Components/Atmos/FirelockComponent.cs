using Content.Server.GameObjects.Components.Doors;
using Content.Server.Interfaces.GameObjects.Components.Doors;
using Content.Shared.GameObjects.Components.Doors;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Content.Server.Atmos;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.Components.Atmos
{
    /// <summary>
    /// Companion component to ServerDoorComponent that handles firelock-specific behavior.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IDoorCheck))]
    public class FirelockComponent : Component, IDoorCheck
    {
        public override string Name => "Firelock";

        public bool EmergencyPressureStop()
        {
            Owner.TryGetComponent<ServerDoorComponent>(out var doorcomponent);

            if (doorcomponent.State == SharedDoorComponent.DoorState.Open && doorcomponent.CanCloseGeneric())
            {
                doorcomponent.Close();
                Owner.GetComponent<AirtightComponent>().AirBlocked = true;
                return true;
            }
            return false;
        }

        public bool OpenCheck()
        {
            return !IsHoldingFire() && !IsHoldingPressure();
        }

        public bool DenyCheck() => false;

        public float? GetPryTime()
        {
            if (IsHoldingFire() || IsHoldingPressure())
            {
                return 1.5f;
            }
            return 0.25f;
        }

        public void OnStartPry(InteractUsingEventArgs eventArgs)
        {
            if (Owner.GetComponent<ServerDoorComponent>().State == SharedDoorComponent.DoorState.Closed && IsHoldingPressure())
            {
                Owner.PopupMessage(eventArgs.User, "A gush of air blows in your face... Maybe you should reconsider.");
            }
        }

        public bool IsHoldingPressure(float threshold = 20)
        {
            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();

            if (!Owner.Transform.Coordinates.TryGetTileAtmosphere(out var tileAtmos))
                return false;

            var gridAtmosphere = atmosphereSystem.GetGridAtmosphere(Owner.Transform.GridID);

            var minMoles = float.MaxValue;
            var maxMoles = 0f;

            foreach (var (_, adjacent) in gridAtmosphere.GetAdjacentTiles(tileAtmos.GridIndices))
            {
                // includeAirBlocked remains false, and therefore Air must be present
                var moles = adjacent.Air!.TotalMoles;
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

            if (!Owner.Transform.Coordinates.TryGetTileAtmosphere(out var tileAtmos))
                return false;

            if (tileAtmos.Hotspot.Valid)
                return true;

            var gridAtmosphere = atmosphereSystem.GetGridAtmosphere(Owner.Transform.GridID);

            foreach (var (_, adjacent) in gridAtmosphere.GetAdjacentTiles(tileAtmos.GridIndices))
            {
                if (adjacent.Hotspot.Valid)
                    return true;
            }

            return false;
        }
    }
}
