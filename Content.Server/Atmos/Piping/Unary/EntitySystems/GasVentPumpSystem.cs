using System;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Visuals;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public class GasVentPumpSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasVentPumpComponent, AtmosDeviceUpdateEvent>(OnGasVentPumpUpdated);
            SubscribeLocalEvent<GasVentPumpComponent, AtmosDeviceDisabledEvent>(OnGasVentPumpLeaveAtmosphere);
        }

        private void OnGasVentPumpUpdated(EntityUid uid, GasVentPumpComponent vent, AtmosDeviceUpdateEvent args)
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

            if (!nodeContainer.TryGetNode(vent.InletName, out PipeNode? pipe))
                return;

            var environment = args.Atmosphere.GetTile(vent.Owner.Transform.Coordinates)!;

            // We're in an air-blocked tile... Do nothing.
            if (environment.Air == null)
                return;

            if (vent.PumpDirection == VentPumpDirection.Releasing)
            {
                appearance?.SetData(VentPumpVisuals.State, VentPumpState.Out);
                var pressureDelta = 10000f;

                if ((vent.PressureChecks & VentPressureBound.ExternalBound) != 0)
                    pressureDelta = MathF.Min(pressureDelta, vent.ExternalPressureBound - environment.Air.Pressure);

                if ((vent.PressureChecks & VentPressureBound.InternalBound) != 0)
                    pressureDelta = MathF.Min(pressureDelta, pipe.Air.Pressure - vent.InternalPressureBound);

                if (pressureDelta > 0 && pipe.Air.Temperature > 0)
                {
                    var transferMoles = pressureDelta * environment.Air.Volume / (pipe.Air.Temperature * Atmospherics.R);

                    environment.AssumeAir(pipe.Air.Remove(transferMoles));
                }
            }
            else if (vent.PumpDirection == VentPumpDirection.Siphoning && environment.Air.Pressure > 0)
            {
                appearance?.SetData(VentPumpVisuals.State, VentPumpState.In);
                var ourMultiplier = pipe.Air.Volume / (environment.Air.Temperature * Atmospherics.R);
                var molesDelta = 10000f * ourMultiplier;

                if ((vent.PressureChecks & VentPressureBound.ExternalBound) != 0)
                    molesDelta = MathF.Min(molesDelta,
                        (environment.Air.Pressure - vent.ExternalPressureBound) * environment.Air.Volume /
                        (environment.Air.Temperature * Atmospherics.R));

                if ((vent.PressureChecks & VentPressureBound.InternalBound) != 0)
                    molesDelta = MathF.Min(molesDelta, (vent.InternalPressureBound - pipe.Air.Pressure) * ourMultiplier);

                if (molesDelta > 0)
                {
                    var removed = environment.Air.Remove(molesDelta);
                    pipe.AssumeAir(removed);
                    environment.Invalidate();
                }
            }
        }

        private void OnGasVentPumpLeaveAtmosphere(EntityUid uid, GasVentPumpComponent component, AtmosDeviceDisabledEvent args)
        {
            if (ComponentManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                appearance.SetData(VentPumpVisuals.State, VentPumpState.Off);
            }
        }
    }
}
