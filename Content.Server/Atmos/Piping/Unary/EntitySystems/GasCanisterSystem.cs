using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Atmos.Piping.Unary.Systems;
using Content.Shared.Cargo;
using Content.Shared.Database;
using Content.Shared.NodeContainer;
using GasCanisterComponent = Content.Shared.Atmos.Piping.Unary.Components.GasCanisterComponent;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems;

public sealed class GasCanisterSystem : SharedGasCanisterSystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasCanisterComponent, AtmosDeviceUpdateEvent>(OnCanisterUpdated);
        SubscribeLocalEvent<GasCanisterComponent, PriceCalculationEvent>(CalculateCanisterPrice);
        SubscribeLocalEvent<GasCanisterComponent, GasAnalyzerScanEvent>(OnAnalyzed);
    }

    /// <summary>
    /// Completely dumps the content of the canister into the world.
    /// </summary>
    public void PurgeContents(EntityUid uid, GasCanisterComponent? canister = null, TransformComponent? transform = null)
    {
        if (!Resolve(uid, ref canister, ref transform))
            return;

        var environment = _atmos.GetContainingMixture((uid, transform), false, true);

        if (environment is not null)
            _atmos.Merge(environment, canister.Air);

        AdminLogger.Add(LogType.CanisterPurged, LogImpact.Medium, $"Canister {ToPrettyString(uid):canister} purged its contents of {canister.Air:gas} into the environment.");
        canister.Air.Clear();
    }

    protected override void DirtyUI(EntityUid uid, GasCanisterComponent? canister = null, NodeContainerComponent? nodeContainer = null)
    {
        if (!Resolve(uid, ref canister, ref nodeContainer))
            return;

        var portStatus = false;
        var tankPressure = 0f;

        if (_nodeContainer.TryGetNode(nodeContainer, canister.PortName, out PipeNode? portNode) && portNode.NodeGroup?.Nodes.Count > 1)
            portStatus = true;

        if (canister.GasTankSlot.Item != null)
        {
            var tank = canister.GasTankSlot.Item.Value;
            var tankComponent = Comp<GasTankComponent>(tank);
            tankPressure = tankComponent.Air.Pressure;
        }

        UI.SetUiState(uid, GasCanisterUiKey.Key,
            new GasCanisterBoundUserInterfaceState(canister.Air.Pressure, portStatus, tankPressure));
    }

    private void OnCanisterUpdated(EntityUid uid, GasCanisterComponent canister, ref AtmosDeviceUpdateEvent args)
    {
        _atmos.React(canister.Air, canister);

        if (!TryComp<NodeContainerComponent>(uid, out var nodeContainer)
            || !TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        if (!_nodeContainer.TryGetNode(nodeContainer, canister.PortName, out PortablePipeNode? portNode))
            return;

        if (portNode.NodeGroup is PipeNet {NodeCount: > 1} net)
        {
            MixContainerWithPipeNet(canister.Air, net.Air);
        }

        // Release valve is open, release gas.
        if (canister.ReleaseValve)
        {
            if (canister.GasTankSlot.Item != null)
            {
                var gasTank = Comp<GasTankComponent>(canister.GasTankSlot.Item.Value);
                _atmos.ReleaseGasTo(canister.Air, gasTank.Air, canister.ReleasePressure);
            }
            else
            {
                var environment = _atmos.GetContainingMixture(uid, args.Grid, args.Map, false, true);
                _atmos.ReleaseGasTo(canister.Air, environment, canister.ReleasePressure);
            }
        }

        // If last pressure is very close to the current pressure, do nothing.
        if (MathHelper.CloseToPercent(canister.Air.Pressure, canister.LastPressure))
            return;

        DirtyUI(uid, canister, nodeContainer);

        canister.LastPressure = canister.Air.Pressure;

        if (canister.Air.Pressure < 10)
        {
            _appearance.SetData(uid, GasCanisterVisuals.PressureState, 0, appearance);
        }
        else if (canister.Air.Pressure < Atmospherics.OneAtmosphere)
        {
            _appearance.SetData(uid, GasCanisterVisuals.PressureState, 1, appearance);
        }
        else if (canister.Air.Pressure < (15 * Atmospherics.OneAtmosphere))
        {
            _appearance.SetData(uid, GasCanisterVisuals.PressureState, 2, appearance);
        }
        else
        {
            _appearance.SetData(uid, GasCanisterVisuals.PressureState, 3, appearance);
        }
    }

    /// <summary>
    /// Mix air from a gas container into a pipe net.
    /// Useful for anything that uses connector ports.
    /// </summary>
    public void MixContainerWithPipeNet(GasMixture containerAir, GasMixture pipeNetAir)
    {
        var buffer = new GasMixture(pipeNetAir.Volume + containerAir.Volume);

        _atmos.Merge(buffer, pipeNetAir);
        _atmos.Merge(buffer, containerAir);

        pipeNetAir.Clear();
        _atmos.Merge(pipeNetAir, buffer);
        pipeNetAir.Multiply(pipeNetAir.Volume / buffer.Volume);

        containerAir.Clear();
        _atmos.Merge(containerAir, buffer);
        containerAir.Multiply(containerAir.Volume / buffer.Volume);
    }

    private void CalculateCanisterPrice(EntityUid uid, GasCanisterComponent component, ref PriceCalculationEvent args)
    {
        args.Price += _atmos.GetPrice(component.Air);
    }

    /// <summary>
    /// Returns the gas mixture for the gas analyzer
    /// </summary>
    private void OnAnalyzed(EntityUid uid, GasCanisterComponent canisterComponent, GasAnalyzerScanEvent args)
    {
        args.GasMixtures ??= new List<(string, GasMixture?)>();
        args.GasMixtures.Add((Name(uid), canisterComponent.Air));
        // if a tank is inserted show it on the analyzer as well
        if (canisterComponent.GasTankSlot.Item != null)
        {
            var tank = canisterComponent.GasTankSlot.Item.Value;
            var tankComponent = Comp<GasTankComponent>(tank);
            args.GasMixtures.Add((Name(tank), tankComponent.Air));
        }
    }
}
