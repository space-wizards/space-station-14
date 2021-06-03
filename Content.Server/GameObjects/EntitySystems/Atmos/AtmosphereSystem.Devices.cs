using Content.Server.Atmos;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.Components.Atmos.Piping;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Physics;

namespace Content.Server.GameObjects.EntitySystems.Atmos
{
    public partial class AtmosphereSystem
    {
        private void InitializeDevices()
        {
            // Atmos devices.
            SubscribeLocalEvent<AtmosDeviceComponent, ComponentInit>(OnDeviceInitialize);
            SubscribeLocalEvent<AtmosDeviceComponent, ComponentShutdown>(OnDeviceShutdown);
            SubscribeLocalEvent<AtmosDeviceComponent, PhysicsBodyTypeChangedEvent>(OnDeviceBodyTypeChanged);
            SubscribeLocalEvent<AtmosDeviceComponent, EntParentChangedMessage>(OnDeviceParentChanged);

            // Atmos unsafe unanchoring.
            SubscribeLocalEvent<AtmosUnsafeUnanchorComponent, BeforeUnanchoredEvent>(OnBeforeUnanchored);
            SubscribeLocalEvent<AtmosUnsafeUnanchorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        }

        #region Devices
        private bool CanJoinAtmosphere(AtmosDeviceComponent component)
        {
            return !component.RequireAnchored || !component.Owner.TryGetComponent(out PhysicsComponent? physics) || physics.BodyType == BodyType.Static;
        }

        public void JoinAtmosphere(AtmosDeviceComponent component)
        {
            if (!CanJoinAtmosphere(component))
                return;

            // We try to get a valid, simulated atmosphere.
            if (!TryGetSimulatedGridAtmosphere(component.Owner.Transform.MapPosition, out var atmosphere))
                return;

            component.Atmosphere = atmosphere;
            atmosphere.AddAtmosDevice(component);
        }

        public void LeaveAtmosphere(AtmosDeviceComponent component)
        {
            component.Atmosphere?.RemoveAtmosDevice(component);
            component.Atmosphere = null;
        }

        public void RejoinAtmosphere(AtmosDeviceComponent component)
        {
            LeaveAtmosphere(component);
            JoinAtmosphere(component);
        }

        private void OnDeviceInitialize(EntityUid uid, AtmosDeviceComponent component, ComponentInit args)
        {
            JoinAtmosphere(component);
        }

        private void OnDeviceShutdown(EntityUid uid, AtmosDeviceComponent component, ComponentShutdown args)
        {
            LeaveAtmosphere(component);
        }

        private void OnDeviceBodyTypeChanged(EntityUid uid, AtmosDeviceComponent component, PhysicsBodyTypeChangedEvent args)
        {
            // Do nothing if the component doesn't require being anchored to function.
            if (!component.RequireAnchored)
                return;

            if (args.New == BodyType.Static)
                JoinAtmosphere(component);
            else
                LeaveAtmosphere(component);
        }

        private void OnDeviceParentChanged(EntityUid uid, AtmosDeviceComponent component, EntParentChangedMessage args)
        {
            RejoinAtmosphere(component);
        }
        #endregion

        #region UnsafeUnanchor
        private void OnUnanchorAttempt(EntityUid uid, AtmosUnsafeUnanchorComponent component, UnanchorAttemptEvent args)
        {
            if (!component.Enabled || !ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodes))
                return;

            if (!component.Owner.Transform.Coordinates.TryGetTileAir(out var environment, EntityManager))
                return;

            foreach (var node in nodes.Nodes.Values)
            {
                if (node is not PipeNode pipe) continue;

                if ((pipe.Air.Pressure - environment.Pressure) > 2 * Atmospherics.OneAtmosphere)
                {
                    args.Delay += 1.5f;
                    args.User?.PopupMessageCursor(Loc.GetString("comp-atmos-unsafe-unanchor-warning"));
                    return; // Show the warning only once.
                }
            }
        }

        private void OnBeforeUnanchored(EntityUid uid, AtmosUnsafeUnanchorComponent component, BeforeUnanchoredEvent args)
        {
            if (!component.Enabled || !ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodes))
                return;

            if (!component.Owner.Transform.Coordinates.TryGetTileAtmosphere(out var environment))
                environment = null;

            var environmentPressure = environment?.Air?.Pressure ?? 0f;
            var environmentVolume = environment?.Air?.Volume ?? Atmospherics.CellVolume;
            var environmentTemperature = environment?.Air?.Volume ?? Atmospherics.TCMB;

            var lost = 0f;
            var timesLost = 0;

            foreach (var node in nodes.Nodes.Values)
            {
                if (node is not PipeNode pipe) continue;

                var difference = pipe.Air.Pressure - environmentPressure;
                lost += difference * environmentVolume / (environmentTemperature * Atmospherics.R);
                timesLost++;
            }

            var sharedLoss = lost / timesLost;
            var buffer = new GasMixture();

            foreach (var node in nodes.Nodes.Values)
            {
                if (node is not PipeNode pipe) continue;

                buffer.Merge(pipe.Air.Remove(sharedLoss));
            }

            environment?.AssumeAir(buffer);
        }
        #endregion
    }
}
