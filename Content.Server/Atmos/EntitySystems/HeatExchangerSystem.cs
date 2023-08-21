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

namespace Content.Server.Atmos.EntitySystems;

public sealed class HeatExchangerSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

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
        var dt = args.dt;
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

        float CXfer = _atmosphereSystem.GetHeatCapacity(xfer);
        if (CXfer < Atmospherics.MinimumHeatCapacity)
            return;

        var radTemp = Atmospherics.TCMB;

        var environment = _atmosphereSystem.GetContainingMixture(uid, true, true);
        bool hasEnv = false;
        float CEnv = 0f;
        if (environment != null)
        {
            CEnv = _atmosphereSystem.GetHeatCapacity(environment);
            hasEnv = CEnv >= Atmospherics.MinimumHeatCapacity && environment.TotalMoles > 0f;
            if (hasEnv)
                radTemp = environment.Temperature;
        }

        // How ΔT' scales in respect to heat transferred
        float TdivQ = 1f / CXfer;
        // Since it's ΔT, also account for the environment's temperature change
        if (hasEnv)
            TdivQ += 1f / CEnv;

        // Radiation
        float dTR = xfer.Temperature - radTemp;
        float dTRA = MathF.Abs(dTR);
        float a0 = tileLoss / MathF.Pow(Atmospherics.T20C, 4);
        // ΔT' = -kΔT^4, k = -ΔT'/ΔT^4
        float kR = comp.alpha * a0 * TdivQ;
        // Based on the fact that ((3t)^(-1/3))' = -(3t)^(-4/3) = -((3t)^(-1/3))^4, and ΔT' = -kΔT^4.
        float dT2R = dTR * MathF.Pow((1f + 3f * kR * dt * dTRA * dTRA * dTRA), -1f/3f);
        float dER = (dTR - dT2R) / TdivQ;
        _atmosphereSystem.AddHeat(xfer, -dER);
        if (hasEnv && environment != null)
        {
            _atmosphereSystem.AddHeat(environment, dER);

            // Convection

            // Positive dT is from pipe to surroundings
            float dT = xfer.Temperature - environment.Temperature;
            // ΔT' = -kΔT, k = -ΔT' / ΔT
            float k = comp.K * TdivQ;
            float dT2 = dT * MathF.Exp(-k * dt);
            float dE = (dT - dT2) / TdivQ;
            _atmosphereSystem.AddHeat(xfer, -dE);
            _atmosphereSystem.AddHeat(environment, dE);
        }

        if (dN > 0)
            _atmosphereSystem.Merge(outlet.Air, xfer);
        else
            _atmosphereSystem.Merge(inlet.Air, xfer);

    }
}
