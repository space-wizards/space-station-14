using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems
{
    [UsedImplicitly]
    public class GasPressurePumpSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasPressurePumpComponent, AtmosDeviceUpdateEvent>(OnPumpUpdated);
            SubscribeLocalEvent<GasPressurePumpComponent, AtmosDeviceDisabledEvent>(OnPumpLeaveAtmosphere);
            SubscribeLocalEvent<GasPressurePumpComponent, ExaminedEvent>(OnExamined);
        }

        private void OnExamined(EntityUid uid, GasPressurePumpComponent pump, ExaminedEvent args)
        {
            if (!pump.Owner.Transform.Anchored || !args.IsInDetailsRange) // Not anchored? Out of range? No status.
                return;

            if (Loc.TryGetString("gas-pressure-pump-system-examined", out var str,
                        ("statusColor", "lightblue"), // TODO: change with pressure?
                        ("pressure", pump.TargetPressure)
            ))
                args.PushMarkup(str);
        }

        private void OnPumpUpdated(EntityUid uid, GasPressurePumpComponent pump, AtmosDeviceUpdateEvent args)
        {
            var appearance = pump.Owner.GetComponentOrNull<AppearanceComponent>();

            if (!pump.Enabled
                || !EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer)
                || !nodeContainer.TryGetNode(pump.InletName, out PipeNode? inlet)
                || !nodeContainer.TryGetNode(pump.OutletName, out PipeNode? outlet))
            {
                appearance?.SetData(PressurePumpVisuals.Enabled, false);
                return;
            }

            var outputStartingPressure = outlet.Air.Pressure;

            if (MathHelper.CloseToPercent(pump.TargetPressure, outputStartingPressure))
            {
                appearance?.SetData(PressurePumpVisuals.Enabled, false);
                return; // No need to pump gas if target has been reached.
            }

            if (inlet.Air.TotalMoles > 0 && inlet.Air.Temperature > 0)
            {
                appearance?.SetData(PressurePumpVisuals.Enabled, true);

                // We calculate the necessary moles to transfer using our good ol' friend PV=nRT.
                var pressureDelta = pump.TargetPressure - outputStartingPressure;
                var transferMoles = pressureDelta * outlet.Air.Volume / inlet.Air.Temperature * Atmospherics.R;

                var removed = inlet.Air.Remove(transferMoles);
                outlet.AssumeAir(removed);
            }
        }

        private void OnPumpLeaveAtmosphere(EntityUid uid, GasPressurePumpComponent component, AtmosDeviceDisabledEvent args)
        {
            if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                appearance.SetData(PressurePumpVisuals.Enabled, false);
            }
        }

    }
}
