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
            SubscribeLocalEvent<AtmosDeviceComponent, PhysicsBodyTypeChangedEvent>(OnDeviceBodyTypeChanged);
            SubscribeLocalEvent<AtmosDeviceComponent, EntParentChangedMessage>(OnDeviceParentChanged);

            // Atmos unsafe unanchoring.
            SubscribeLocalEvent<AtmosUnsafeUnanchorComponent, UnanchoredEvent>(OnUnanchored);
            SubscribeLocalEvent<AtmosUnsafeUnanchorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        }

        private void OnDeviceBodyTypeChanged(EntityUid uid, AtmosDeviceComponent component, PhysicsBodyTypeChangedEvent args)
        {
            // Do nothing if the component doesn't require being anchored to function.
            if (!component.RequireAnchored)
                return;

            if (args.New == BodyType.Static)
                component.JoinAtmosphere();
            else
                component.LeaveAtmosphere();
        }

        private void OnDeviceParentChanged(EntityUid uid, AtmosDeviceComponent component, EntParentChangedMessage args)
        {
            component.RejoinAtmosphere();
        }

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
                    args.User?.PopupMessageCursor(Loc.GetString("comp-atmos-unsafe-unanchor-warning"));
                    return; // Show the warning only once.
                }
            }
        }

        private void OnUnanchored(EntityUid uid, AtmosUnsafeUnanchorComponent component, UnanchoredEvent args)
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
    }
}
