using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Robust.Shared.Timing;

namespace Content.Server._00OuterRim.Power.Gas;

/// <summary>
/// This handles...
/// </summary>
public sealed class GasPowerProviderSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GasPowerProviderComponent, AtmosDeviceUpdateEvent>(OnDeviceUpdated);
        //SubscribeLocalEvent<GasPowerProviderComponent, AtmosDeviceDisabledEvent>(OnDeviceDisabled);
    }

    private void OnDeviceDisabled(EntityUid uid, GasPowerProviderComponent component, AtmosDeviceDisabledEvent args)
    {
        RaiseLocalEvent(uid, new PowerChangedEvent(true, 0));
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

        if (!TryComp(uid, out AtmosDeviceComponent? device)
            || !TryComp(uid, out NodeContainerComponent? nodeContainer)
            || !nodeContainer.TryGetNode("pipe", out PipeNode? pipe))
        {
            return;
        }

        if (pipe.Air.Temperature <= component.MaxTemperature)
        {
            if (pipe.Air.Moles[(int) Shared.Atmos.Gas.Plasma] > component.PlasmaMolesConsumedSec * timeDelta)
            {
                pipe.Air.AdjustMoles(Shared.Atmos.Gas.Plasma, -component.PlasmaMolesConsumedSec * timeDelta);
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
                pipe.Air.Remove((pres * 100.0f) / (Shared.Atmos.Atmospherics.R * pipe.Air.Temperature));
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
            RaiseLocalEvent(uid, new PowerChangedEvent(state, 0));
        }

        return;
    }
}
