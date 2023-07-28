using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Robust.Shared.Timing;

namespace Content.Server.Power.Generator;

/// <summary>
/// This handles gas power providers, allowing devices to accept power in the form of plasma or high pressure gas.
/// </summary>
public sealed class GasPowerProviderSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GasPowerProviderComponent, AtmosDeviceUpdateEvent>(OnDeviceUpdated);
    }

    private void OnDeviceUpdated(EntityUid uid, GasPowerProviderComponent component, AtmosDeviceUpdateEvent args)
    {
        if (component.LastProcess == TimeSpan.Zero)
        {
            component.LastProcess = _gameTiming.CurTime;
            return; // Skip tick 0.
        }

        var timeDelta =  (float)(_gameTiming.CurTime - component.LastProcess).TotalSeconds;
        component.LastProcess = _gameTiming.CurTime;

        if (!HasComp<AtmosDeviceComponent>(uid)
            || !TryComp<NodeContainerComponent>(uid, out var nodeContainer)
            || !_nodeContainer.TryGetNode<PipeNode>(nodeContainer, "pipe", out var pipe))
        {
            return;
        }

        if (pipe.Air.Temperature <= component.MaxTemperature)
        {
            if (pipe.Air.Moles[(int) component.TargetGas] > component.MolesConsumedSec * timeDelta)
            {
                pipe.Air.AdjustMoles(component.TargetGas, -component.MolesConsumedSec * timeDelta);
                SetPowered(uid, component, true);
            }
            else
            {
                SetPowered(uid, component, false);
            }
        }
        else
        {
            var pres = component.PressureConsumedSec * timeDelta;
            if (pipe.Air.Pressure >= pres)
            {
                var res = pipe.Air.Remove(pres * 100.0f / (Atmospherics.R * pipe.Air.Temperature));
                if (component.OffVentGas)
                {
                    var mix = _atmosphereSystem.GetContainingMixture(uid, false, true);
                    if (mix is not null)
                        _atmosphereSystem.Merge(res, mix);
                }

                SetPowered(uid, component, true);
            }
            else
            {
                SetPowered(uid, component, false);
            }
        }
    }

    private void SetPowered(EntityUid uid, GasPowerProviderComponent comp, bool state)
    {
        if (state != comp.Powered)
        {
            comp.Powered = state;
            var ev = new PowerChangedEvent(state, 0);
            RaiseLocalEvent(uid, ref ev);
        }
    }
}
