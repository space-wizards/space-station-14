using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Trinary.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Nodes;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Audio;
using Content.Shared.NodeContainer.Systems;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Piping.Trinary.EntitySystems;

[UsedImplicitly]
public sealed partial class PressureControlledValveSystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private NodeContainerSystem _nodeContainer = default!;

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

    private void OnUpdate(EntityUid uid, PressureControlledValveComponent comp, ref AtmosDeviceUpdateEvent args)
    {
        if (!_nodeContainer.TryGetNodes(
                uid,
                comp.InletName,
                comp.ControlName,
                comp.OutletName,
                out PipeNode? inletNode,
                out PipeNode? controlNode,
                out PipeNode? outletNode)
            || inletNode.PipeNet == null
            || controlNode.PipeNet == null
            || outletNode.PipeNet == null)
        {
            _ambientSoundSystem.SetAmbience(uid, false);
            comp.Enabled = false;
            return;
        }

        var inletNodeAir = inletNode.PipeNet.Value.Comp.Air;
        var controlNodeAir = controlNode.PipeNet.Value.Comp.Air;
        var outletNodeAir = controlNode.PipeNet.Value.Comp.Air;

        // If output is higher than input, flip input/output to enable bidirectional flow.
        if (outletNodeAir.Pressure > inletNodeAir.Pressure)
        {
            PipeNode temp = outletNode;
            outletNode = inletNode;
            inletNode = temp;
        }

        float control = (controlNodeAir.Pressure - outletNodeAir.Pressure) - comp.Threshold;
        float transferRate;
        if (control < 0)
        {
            comp.Enabled = false;
            transferRate = 0;
        }
        else
        {
            comp.Enabled = true;
            transferRate = Math.Min(control * comp.Gain, comp.MaxTransferRate * _atmosphereSystem.PumpSpeedup());
        }
        UpdateAppearance(uid, comp);

        // We multiply the transfer rate in L/s by the seconds passed since the last process to get the liters.
        var transferVolume = transferRate * args.dt;
        if (transferVolume <= 0)
        {
            _ambientSoundSystem.SetAmbience(uid, false);
            return;
        }

        // clamp to equalization so we don't overshoot (happens with silly euler)
        var maxFrac = _atmosphereSystem.FractionToEqualizePressure(inletNodeAir, outletNodeAir);
        var maxVol = inletNodeAir.Volume * maxFrac;
        var clampedVolume = Math.Min(transferVolume, maxVol);

        _ambientSoundSystem.SetAmbience(uid, true);
        var removed = inletNodeAir.RemoveVolume(clampedVolume);
        _atmosphereSystem.Merge(outletNodeAir, removed);
    }

    private void OnFilterLeaveAtmosphere(EntityUid uid, PressureControlledValveComponent comp, ref AtmosDeviceDisabledEvent args)
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
