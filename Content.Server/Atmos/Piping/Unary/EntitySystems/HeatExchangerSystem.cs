using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Atmos.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping;
using Content.Shared.CCVar;
using Content.Shared.Interaction;
using Robust.Shared.Configuration;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems;

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
        if (!TryComp(uid, out NodeContainerComponent? nodeContainer) ||
            !TryComp(uid, out AtmosDeviceComponent? device) ||
            !_nodeContainer.TryGetNode(nodeContainer, comp.InletName, out PipeNode? inlet))
        {
            return;
        }

        // heat capacity of the gas inside the radiator, skip processing if there is none
        var heatCapacity = _atmosphereSystem.GetHeatCapacity(inlet.Air);
        if (heatCapacity < Atmospherics.MinimumHeatCapacity)
            return;

        var dt = args.dt;

        var radTemp = Atmospherics.TCMB;

        // get the heat capacity of the environment.
        // if there is no environment then it will radiate heat into space and not exchange with anything.
        var environment = _atmosphereSystem.GetContainingMixture(uid, true, true);
        bool hasEnv = false;
        var envCapacity = 0f;
        if (environment != null)
        {
            envCapacity = _atmosphereSystem.GetHeatCapacity(environment);
            hasEnv = envCapacity >= Atmospherics.MinimumHeatCapacity && environment.TotalMoles > 0f;
            if (hasEnv)
                radTemp = environment.Temperature;
        }

        // How ΔT' scales in respect to heat transferred
        float TdivQ = 1f / heatCapacity;
        // Since it's ΔT, also account for the environment's temperature change
        if (hasEnv)
            TdivQ += 1f / envCapacity;

        // Radiation
        float dTR = inlet.Air.Temperature - radTemp;
        float dTRA = MathF.Abs(dTR);
        float a0 = tileLoss / MathF.Pow(Atmospherics.T20C, 4);
        // ΔT' = -kΔT^4, k = -ΔT'/ΔT^4
        float kR = comp.alpha * a0 * TdivQ;
        // Based on the fact that ((3t)^(-1/3))' = -(3t)^(-4/3) = -((3t)^(-1/3))^4, and ΔT' = -kΔT^4.
        float dT2R = dTR * MathF.Pow((1f + 3f * kR * dt * dTRA * dTRA * dTRA), -1f/3f);
        float dER = (dTR - dT2R) / TdivQ;
        _atmosphereSystem.AddHeat(inlet.Air, -dER);
        if (hasEnv && environment != null)
        {
            _atmosphereSystem.AddHeat(environment, dER);

            // Convection

            // Positive dT is from pipe to surroundings
            float dT = inlet.Air.Temperature - environment.Temperature;
            // ΔT' = -kΔT, k = -ΔT' / ΔT
            float k = comp.K * TdivQ;
            float dT2 = dT * MathF.Exp(-k * dt);
            float dE = (dT - dT2) / TdivQ;
            _atmosphereSystem.AddHeat(inlet.Air, -dE);
            _atmosphereSystem.AddHeat(environment, dE);
        }
    }
}
