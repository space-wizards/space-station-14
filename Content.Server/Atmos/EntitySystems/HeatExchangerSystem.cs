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
    [Dependency] private readonly SharedTransformSystem _transform = default!;

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

    private void OnAtmosUpdate(EntityUid uid, HeatExchangerComponent comp, ref AtmosDeviceUpdateEvent args)
    {
        if (!TryComp(uid, out NodeContainerComponent? nodeContainer)
                || !TryComp(uid, out AtmosDeviceComponent? device)
                || !_nodeContainer.TryGetNode(nodeContainer, comp.InletName, out PipeNode? inlet)
                || !_nodeContainer.TryGetNode(nodeContainer, comp.OutletName, out PipeNode? outlet))
        {
            return;
        }

        // make sure that the tile the device is on isn't blocked by a wall or something similar.
        var xform = Transform(uid);
        if (_transform.TryGetGridTilePosition(uid, out var tile))
        {
            // TryGetGridTilePosition() already returns false if GridUid is null, but the null checker isn't smart enough yet
            if (xform.GridUid != null && _atmosphereSystem.IsTileAirBlocked(xform.GridUid.Value, tile))
            {
                return;
            }
        }

        var dt = args.dt;

        // Let n = moles(inlet) - moles(outlet), really a Δn
        var P = inlet.Air.Pressure - outlet.Air.Pressure; // really a ΔP
        // Such that positive P causes flow from the inlet to the outlet.

        // We want moles transferred to be proportional to the pressure difference, i.e.
        // dn/dt = G*P

        // To solve this we need to write dn in terms of P. Since PV=nRT, dP/dn=RT/V.
        // This assumes that the temperature change from transferring dn moles is negligible.
        // Since we have P=Pi-Po, then dP/dn = dPi/dn-dPo/dn = R(Ti/Vi - To/Vo):
        float dPdn = Atmospherics.R * (outlet.Air.Temperature / outlet.Air.Volume + inlet.Air.Temperature / inlet.Air.Volume);

        // Multiplying both sides of the differential equation by dP/dn:
        // dn/dt * dP/dn = dP/dt = G*P * (dP/dn)
        // Which is a first-order linear differential equation with constant (heh...) coefficients:
        // dP/dt + kP = 0, where k = -G*(dP/dn).
        // This differential equation has a closed-form solution, namely:
        float Pfinal = P * MathF.Exp(-comp.G * dPdn * dt);

        // Finally, back out n, the moles transferred in this tick:
        float n = (P - Pfinal) / dPdn;

        GasMixture xfer;
        if (n > 0)
            xfer = inlet.Air.Remove(n);
        else
            xfer = outlet.Air.Remove(-n);

        float CXfer = _atmosphereSystem.GetHeatCapacity(xfer, true);
        if (CXfer < Atmospherics.MinimumHeatCapacity)
            return;

        var radTemp = Atmospherics.TCMB;

        var environment = _atmosphereSystem.GetContainingMixture(uid, true, true);
        bool hasEnv = false;
        float CEnv = 0f;
        if (environment != null)
        {
            CEnv = _atmosphereSystem.GetHeatCapacity(environment, true);
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

        if (n > 0)
            _atmosphereSystem.Merge(outlet.Air, xfer);
        else
            _atmosphereSystem.Merge(inlet.Air, xfer);

    }
}
