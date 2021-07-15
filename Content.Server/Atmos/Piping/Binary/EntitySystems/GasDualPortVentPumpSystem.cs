using System;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Visuals;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems
{
    [UsedImplicitly]
    public class GasDualPortVentPumpSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasDualPortVentPumpComponent, AtmosDeviceUpdateEvent>(OnGasDualPortVentPumpUpdated);
            SubscribeLocalEvent<GasDualPortVentPumpComponent, AtmosDeviceDisabledEvent>(OnGasDualPortVentPumpDisabled);
        }

        private void OnGasDualPortVentPumpUpdated(EntityUid uid, GasDualPortVentPumpComponent vent, AtmosDeviceUpdateEvent args)
        {
            var appearance = vent.Owner.GetComponentOrNull<AppearanceComponent>();

            if (vent.Welded)
            {
                appearance?.SetData(VentPumpVisuals.State, VentPumpState.Welded);
                return;
            }

            appearance?.SetData(VentPumpVisuals.State, VentPumpState.Off);

            if (!vent.Enabled)
                return;

            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(vent.InletName, out PipeNode? inlet)
            || !nodeContainer.TryGetNode(vent.OutletName, out PipeNode? outlet))
                return;

            var environment = args.Atmosphere.GetTile(vent.Owner.Transform.Coordinates)!;

            // We're in an air-blocked tile... Do nothing.
            if (environment.Air == null)
                return;

            if (vent.PumpDirection == VentPumpDirection.Releasing)
            {
                appearance?.SetData(VentPumpVisuals.State, VentPumpState.Out);
                var pressureDelta = 10000f;

                if ((vent.PressureChecks & DualPortVentPressureBound.ExternalBound) != 0)
                    pressureDelta = MathF.Min(pressureDelta, (vent.ExternalPressureBound - environment.Air.Pressure));

                if ((vent.PressureChecks & DualPortVentPressureBound.InputMinimum) != 0)
                    pressureDelta = MathF.Min(pressureDelta, (inlet.Air.Pressure - vent.InputPressureMin));

                if (pressureDelta > 0 && inlet.Air.Temperature > 0)
                {
                    var transferMoles = pressureDelta * environment.Air.Volume / inlet.Air.Temperature * Atmospherics.R;
                    var removed = inlet.Air.Remove(transferMoles);
                    environment.AssumeAir(removed);
                }
            }
            else if (vent.PumpDirection == VentPumpDirection.Siphoning && environment.Air.Pressure > 0f)
            {
                appearance?.SetData(VentPumpVisuals.State, VentPumpState.In);
                var ourMultiplier = outlet.Air.Volume / environment.Air.Temperature * Atmospherics.R;
                var molesDelta = 10000 * ourMultiplier;

                if ((vent.PressureChecks & DualPortVentPressureBound.ExternalBound) != 0)
                    molesDelta =
                        MathF.Min(molesDelta,
                            (environment.Air.Pressure - vent.OutputPressureMax) * environment.Air.Volume / (environment.Air.Temperature * Atmospherics.R));

                if ((vent.PressureChecks &DualPortVentPressureBound.InputMinimum) != 0)
                    molesDelta = MathF.Min(molesDelta, (vent.InputPressureMin - outlet.Air.Pressure) * ourMultiplier);

                if (molesDelta > 0)
                {
                    var removed = environment.Air.Remove(molesDelta);

                    Get<AtmosphereSystem>().Merge(outlet.Air, removed);
                    environment.Invalidate();
                }
            }
        }

        private void OnGasDualPortVentPumpDisabled(EntityUid uid, GasDualPortVentPumpComponent vent, AtmosDeviceDisabledEvent args)
        {
            if (ComponentManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                appearance.SetData(VentPumpVisuals.State, VentPumpState.Off);
            }
        }
    }
}
