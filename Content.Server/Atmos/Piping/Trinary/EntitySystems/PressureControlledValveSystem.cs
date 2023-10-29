using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.EntitySystems;
using Content.Server.Atmos.Piping.Trinary.Components;
using Content.Server.Nodes.EntitySystems;
using Content.Shared.Atmos.Piping;
using Content.Shared.Audio;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Piping.Trinary.EntitySystems
{
    [UsedImplicitly]
    public sealed class PressureControlledValveSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly NodeGraphSystem _nodeSystem = default!;
        [Dependency] private readonly AtmosPipeNetSystem _pipeNodeSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PressureControlledValveComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<PressureControlledValveComponent, AtmosDeviceUpdateEvent>(OnUpdate);
            SubscribeLocalEvent<PressureControlledValveComponent, AtmosDeviceDisabledEvent>(OnFilterLeaveAtmosphere);
        }

        private void OnInit(EntityUid uid, PressureControlledValveComponent comp, ComponentInit args)
        {
            UpdateAppearance(uid, comp);
        }

        private void OnUpdate(EntityUid uid, PressureControlledValveComponent comp, AtmosDeviceUpdateEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out AtmosDeviceComponent? device)
            || !_nodeSystem.TryGetNode<AtmosPipeNodeComponent>(uid, comp.InletName, out var inletId, out var inletNode, out var inlet)
            || !_pipeNodeSystem.TryGetGas(inletId, out var inletGas, inlet, inletNode)
            || !_nodeSystem.TryGetNode<AtmosPipeNodeComponent>(uid, comp.ControlName, out var controlId, out var controlNode, out var control)
            || !_pipeNodeSystem.TryGetGas(controlId, out var controlGas, control, controlNode)
            || !_nodeSystem.TryGetNode<AtmosPipeNodeComponent>(uid, comp.OutletName, out var outletId, out var outletNode, out var outlet)
            || !_pipeNodeSystem.TryGetGas(outletId, out var outletGas, outlet, outletNode))
            {
                comp.Enabled = false;
                _ambientSoundSystem.SetAmbience(uid, false);
                return;
            }

            // If output is higher than input, flip input/output to enable bidirectional flow.
            if (outletGas.Pressure > inletGas.Pressure)
                (inletGas, outletGas) = (outletGas, inletGas);

            float controlDelta = (controlGas.Pressure - outletGas.Pressure) - comp.Threshold;
            float transferRate;
            if (controlDelta < 0)
            {
                comp.Enabled = false;
                transferRate = 0;
            }
            else
            {
                comp.Enabled = true;
                transferRate = Math.Min(controlDelta * comp.Gain, comp.MaxTransferRate);
            }
            UpdateAppearance(uid, comp);

            // We multiply the transfer rate in L/s by the seconds passed since the last process to get the liters.
            var transferVolume = transferRate * args.dt;
            if (transferVolume <= 0)
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                return;
            }

            _ambientSoundSystem.SetAmbience(uid, true);
            var removed = inletGas.RemoveVolume(transferVolume);
            _atmosphereSystem.Merge(outletGas, removed);
        }

        private void OnFilterLeaveAtmosphere(EntityUid uid, PressureControlledValveComponent comp, AtmosDeviceDisabledEvent args)
        {
            comp.Enabled = false;
            UpdateAppearance(uid, comp);
            _ambientSoundSystem.SetAmbience(uid, false);
        }

        private void UpdateAppearance(EntityUid uid, PressureControlledValveComponent? comp = null, AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref comp, ref appearance, false))
                return;

            _appearance.SetData(uid, FilterVisuals.Enabled, comp.Enabled, appearance);
        }
    }
}
