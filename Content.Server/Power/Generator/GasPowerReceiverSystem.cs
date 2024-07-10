using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Atmos;

namespace Content.Server.Power.Generator;

/// <summary>
/// This handles gas power receivers, allowing devices to accept power in the form of a gas.
/// </summary>
public sealed class GasPowerReceiverSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GasPowerReceiverComponent, AtmosDeviceUpdateEvent>(OnDeviceUpdated);
    }

    private void OnDeviceUpdated(EntityUid uid, GasPowerReceiverComponent component, ref AtmosDeviceUpdateEvent args)
    {
        var timeDelta = args.dt;

        if (!_nodeContainer.TryGetNode(uid, "pipe", out PipeNode? pipe))
            return;

        // if we're below the max temperature, then we are simply consuming our target gas
        if (pipe.Air.Temperature <= component.MaxTemperature)
        {
            // we have enough gas, so we consume it and are powered
            if (pipe.Air[(int) component.TargetGas] > component.MolesConsumedSec * timeDelta)
            {
                pipe.Air.AdjustMoles(component.TargetGas, -component.MolesConsumedSec * timeDelta);
                SetPowered(uid, component, true);
            }
            else // we do not have enough gas, so we power off
            {
                SetPowered(uid, component, false);
            }
        }
        else // we are exceeding the max temp and are now operating in pressure mode
        {
            var pres = component.PressureConsumedSec * timeDelta;
            if (pipe.Air.Pressure >= pres)
            {
                // remove gas from the pipe
                var res = pipe.Air.Remove(pres * 100.0f / (Atmospherics.R * pipe.Air.Temperature));
                if (component.OffVentGas)
                {
                    // eject the gas into the atmosphere
                    var mix = _atmosphereSystem.GetContainingMixture(uid, args.Grid, args.Map, false, true);
                    if (mix is not null)
                        _atmosphereSystem.Merge(res, mix);
                }

                SetPowered(uid, component, true);
            }
            else // if we do not have high enough pressure to operate, power off
            {
                SetPowered(uid, component, false);
            }
        }
    }

    private void SetPowered(EntityUid uid, GasPowerReceiverComponent comp, bool state)
    {
        if (state != comp.Powered)
        {
            comp.Powered = state;
            var ev = new PowerChangedEvent(state, 0);
            RaiseLocalEvent(uid, ref ev);
        }
    }
}
