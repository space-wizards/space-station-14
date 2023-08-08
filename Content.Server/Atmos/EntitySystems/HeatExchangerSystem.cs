using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.NodeContainer;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos;
using Content.Shared.CCVar;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.EntitySystems;

public sealed class HeatExchangerSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private IGameTiming _gameTiming = default!;

    float tileLoss;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeatExchangerComponent, AtmosDeviceUpdateEvent>(OnAtmosUpdate);

        // Getting CVars is expensive, don't do it every tick
        _cfg.OnValueChanged(CCVars.SuperconductionTileLoss, CacheTileLoss, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(CCVars.SuperconductionTileLoss, CacheTileLoss);
    }

    private void CacheTileLoss(float val)
    {
        tileLoss = val;
    }

    private void OnAtmosUpdate(EntityUid uid, HeatExchangerComponent comp, AtmosDeviceUpdateEvent args)
    {
        if (!TryComp(uid, out NodeContainerComponent? nodeContainer)
                || !TryComp(uid, out AtmosDeviceComponent? device)
                || !_nodeContainer.TryGetNode(nodeContainer, comp.InletName, out PipeNode? inlet)
                || !_nodeContainer.TryGetNode(nodeContainer, comp.OutletName, out PipeNode? outlet))
        {
            return;
        }

        // Positive dN flows from inlet to outlet
        var dt = (float)(_gameTiming.CurTime - device.LastProcess).TotalSeconds;
        var dP = inlet.Air.Pressure - outlet.Air.Pressure;

        // What we want is dN/dt = G*dP (first-order constant-coefficient differential equation w.r.t. P).
        // However, by approximating dN = G*dP*dt using Forward Euler dN can be larger than all of the gas
        // available for sufficiently large dP. However, we know that dN cannot exceed:
        //
        //   dNMax = (n2T2/V2 - n1T1/V1)/(T2/V1 + T2/V2) = Δp/R/T2/(1/V1 + 1/V2)
        //
        // Because that is the amount of gas that needs to be transferred to equalize pressures [citation needed].
        // So we just limit abs(dN) < abs(dNMax).
        //
        // Also, by factoring out dP we can avoid taking any abs().
        // transferLimit below is equal to dNMax/dP
        var transferLimit = 1/Atmospherics.R/outlet.Air.Temperature/(1/inlet.Air.Volume + 1/outlet.Air.Volume);
        var dN = dP*Math.Min(comp.G*dt, transferLimit);

        GasMixture xfer;
        if (dN > 0)
            xfer = inlet.Air.Remove(dN);
        else
            xfer = outlet.Air.Remove(-dN);

        var radTemp = Atmospherics.TCMB;

        // Convection
        var environment = _atmosphereSystem.GetContainingMixture(uid, true, true);
        if (environment != null && environment.TotalMoles != 0)
        {
            radTemp = environment.Temperature;

            // Positive dT is from pipe to surroundings
            var dT = xfer.Temperature - environment.Temperature;
            var dE = comp.K * dT * dt;
            var envLim = Math.Abs(_atmosphereSystem.GetHeatCapacity(environment) * dT * dt);
            var xferLim = Math.Abs(_atmosphereSystem.GetHeatCapacity(xfer) * dT * dt);
            var dEactual = Math.Sign(dE) * Math.Min(Math.Abs(dE), Math.Min(envLim, xferLim));
            _atmosphereSystem.AddHeat(xfer, -dEactual);
            _atmosphereSystem.AddHeat(environment, dEactual);
        }

        // Radiation
        float dTR = xfer.Temperature - radTemp;
        float a0 = tileLoss / MathF.Pow(Atmospherics.T20C, 4);
        float dER = comp.alpha * a0 * MathF.Pow(dTR, 4) * dt;
        _atmosphereSystem.AddHeat(xfer, -dER);

        if (dN > 0)
            _atmosphereSystem.Merge(outlet.Air, xfer);
        else
            _atmosphereSystem.Merge(inlet.Air, xfer);

    }
}
