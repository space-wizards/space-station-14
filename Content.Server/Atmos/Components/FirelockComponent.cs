#nullable enable
using Content.Server.Atmos.EntitySystems;
using Content.Server.Doors;
using Content.Server.Doors.Components;
using Content.Shared.Doors;
using Content.Shared.Interaction;
using Content.Shared.Notification.Managers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Atmos.Components
{
    /// <summary>
    /// Companion component to ServerDoorComponent that handles firelock-specific behavior -- primarily prying, and not being openable on open-hand click.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IDoorCheck))]
    public class FirelockComponent : Component, IDoorCheck
    {
        public override string Name => "Firelock";

        [ComponentDependency]
        private readonly ServerDoorComponent? _doorComponent = null;

        public bool EmergencyPressureStop()
        {
            if (_doorComponent != null && _doorComponent.State == SharedDoorComponent.DoorState.Open && _doorComponent.CanCloseGeneric())
            {
                _doorComponent.Close();
                if (Owner.TryGetComponent(out AirtightComponent? airtight))
                {
                    airtight.AirBlocked = true;
                }
                return true;
            }
            return false;
        }

        bool IDoorCheck.OpenCheck()
        {
            return !IsHoldingFire() && !IsHoldingPressure();
        }

        bool IDoorCheck.DenyCheck() => false;

        float? IDoorCheck.GetPryTime()
        {
            if (IsHoldingFire() || IsHoldingPressure())
            {
                return 1.5f;
            }
            return null;
        }

        bool IDoorCheck.BlockActivate(ActivateEventArgs eventArgs) => true;

        void IDoorCheck.OnStartPry(InteractUsingEventArgs eventArgs)
        {
            if (_doorComponent == null || _doorComponent.State != SharedDoorComponent.DoorState.Closed)
            {
                return;
            }

            if (IsHoldingPressure())
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("firelock-component-is-holding-pressure-message"));
            }
            else if (IsHoldingFire())
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("firelock-component-is-holding-fire-message"));
            }
        }

        public bool IsHoldingPressure(float threshold = 20)
        {
            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();

            var gridAtmosphere = atmosphereSystem.GetGridAtmosphere(Owner.Transform.Coordinates);

            var minMoles = float.MaxValue;
            var maxMoles = 0f;

            foreach (var (_, adjacent) in gridAtmosphere.GetAdjacentTiles(Owner.Transform.Coordinates))
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

            var gridAtmosphere = atmosphereSystem.GetGridAtmosphere(Owner.Transform.Coordinates);

            foreach (var (_, adjacent) in gridAtmosphere.GetAdjacentTiles(tileAtmos.GridIndices))
            {
                if (adjacent.Hotspot.Valid)
                    return true;
            }

            return false;
        }
    }
}
