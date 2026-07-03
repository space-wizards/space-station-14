using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Binary.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Nodes;
using Content.Shared.Examine;
using Content.Shared.NodeContainer.Systems;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems;

[UsedImplicitly]
public sealed partial class GasPassiveGateSystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private NodeContainerSystem _nodeContainer = default!;
    [Dependency] private EntityQuery<PipeNetComponent> _pipeNetQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasPassiveGateComponent, AtmosDeviceUpdateEvent>(OnPassiveGateUpdated);
        SubscribeLocalEvent<GasPassiveGateComponent, ExaminedEvent>(OnExamined);
    }

    private void OnPassiveGateUpdated(EntityUid uid, GasPassiveGateComponent gate, ref AtmosDeviceUpdateEvent args)
    {
        if (!_nodeContainer.TryGetNodes(uid, gate.InletName, gate.OutletName, out PipeNode? inlet, out PipeNode? outlet)
            || inlet.PipeNet == null
            || outlet.PipeNet == null)
            return;

        var inletAir = inlet.PipeNet.Value.Comp.Air;
        var outletAir = outlet.PipeNet.Value.Comp.Air;

        // ReSharper disable thrice InconsistentNaming
        var P1 = inletAir.Pressure;
        var P2 = outletAir.Pressure;
        var V1 = inletAir.Volume;
        var pressureDelta = P1 - P2;

        var dt = args.dt;
        float dV = 0;
        if (pressureDelta > 0 && P1 > 0)
        {
            var transferFrac = _atmosphereSystem.FractionToEqualizePressure(inletAir, outletAir);
            dV = transferFrac * V1;

            // Actually transfer the gas.
            _atmosphereSystem.Merge(outletAir, inletAir.RemoveRatio(transferFrac));
        }

        gate.FlowRate = AtmosphereSystem.ExponentialMovingAverage(dV, gate.FlowRate, dt);
    }

    private void OnExamined(Entity<GasPassiveGateComponent> gate, ref ExaminedEvent args)
    {
        if (!Transform(gate).Anchored || !args.IsInDetailsRange) // Not anchored? Out of range? No status.
            return;

        var str = Loc.GetString("gas-passive-gate-examined", ("flowRate", $"{gate.Comp.FlowRate:0.#}"));
        args.PushMarkup(str);
    }
}
