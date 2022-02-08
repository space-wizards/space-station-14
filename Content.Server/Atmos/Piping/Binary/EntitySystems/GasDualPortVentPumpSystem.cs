using System;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Atmos.Visuals;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems
{
    [UsedImplicitly]
    public class GasDualPortVentPumpSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasDualPortVentPumpComponent, AtmosDeviceUpdateEvent>(OnGasDualPortVentPumpUpdated);
            SubscribeLocalEvent<GasDualPortVentPumpComponent, AtmosDeviceDisabledEvent>(OnGasDualPortVentPumpDisabled);
        }

        private void OnGasDualPortVentPumpUpdated(EntityUid uid, GasDualPortVentPumpComponent vent, AtmosDeviceUpdateEvent args)
        {
            var appearance = EntityManager.GetComponentOrNull<AppearanceComponent>(vent.Owner);

            if (vent.Welded)
            {
                appearance?.SetData(VentPumpVisuals.State, VentPumpState.Welded);
                return;
            }

            if (!vent.Enabled
            || !EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer)
            || !nodeContainer.TryGetNode(vent.InletName, out PipeNode? inlet)
            || !nodeContainer.TryGetNode(vent.OutletName, out PipeNode? outlet))
            {
                appearance?.SetData(VentPumpVisuals.State, VentPumpState.Off);
                return;
            }

            var environment = _atmosphereSystem.GetTileMixture(EntityManager.GetComponent<TransformComponent>(vent.Owner).Coordinates, true);

            // We're in an air-blocked tile... Do nothing.
            if (environment == null)
            {
                appearance?.SetData(VentPumpVisuals.State, VentPumpState.Off);
                return;
            }

            if (vent.PumpDirection == VentPumpDirection.Releasing)
            {
                appearance?.SetData(VentPumpVisuals.State, VentPumpState.Out);
                var pressureDelta = 10000f;

                if ((vent.PressureChecks & DualPortVentPressureBound.ExternalBound) != 0)
                    pressureDelta = MathF.Min(pressureDelta, (vent.ExternalPressureBound - environment.Pressure));

                if ((vent.PressureChecks & DualPortVentPressureBound.InputMinimum) != 0)
                    pressureDelta = MathF.Min(pressureDelta, (inlet.Air.Pressure - vent.InputPressureMin));

                if (pressureDelta > 0 && inlet.Air.Temperature > 0)
                {
                    var transferMoles = pressureDelta * environment.Volume / inlet.Air.Temperature * Atmospherics.R;
                    var removed = inlet.Air.Remove(transferMoles);
                    _atmosphereSystem.Merge(environment, removed);
                }
            }
            else if (vent.PumpDirection == VentPumpDirection.Siphoning && environment.Pressure > 0f)
            {
                appearance?.SetData(VentPumpVisuals.State, VentPumpState.In);
                var ourMultiplier = outlet.Air.Volume / environment.Temperature * Atmospherics.R;
                var molesDelta = 10000 * ourMultiplier;

                if ((vent.PressureChecks & DualPortVentPressureBound.ExternalBound) != 0)
                    molesDelta =
                        MathF.Min(molesDelta,
                            (environment.Pressure - vent.OutputPressureMax) * environment.Volume / (environment.Temperature * Atmospherics.R));

                if ((vent.PressureChecks &DualPortVentPressureBound.InputMinimum) != 0)
                    molesDelta = MathF.Min(molesDelta, (vent.InputPressureMin - outlet.Air.Pressure) * ourMultiplier);

                if (molesDelta > 0)
                {
                    var removed = environment.Remove(molesDelta);

                    _atmosphereSystem.Merge(outlet.Air, removed);
                }
            }
        }

        private void OnGasDualPortVentPumpDisabled(EntityUid uid, GasDualPortVentPumpComponent vent, AtmosDeviceDisabledEvent args)
        {
            if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                appearance.SetData(VentPumpVisuals.State, VentPumpState.Off);
            }
        }
    }
}
