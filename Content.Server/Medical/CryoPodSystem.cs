using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Server.Medical.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Medical.Cryogenics;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.Medical;

public sealed partial class CryoPodSystem : SharedCryoPodSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly GasCanisterSystem _gasCanisterSystem = default!;
    [Dependency] private readonly GasAnalyzerSystem _gasAnalyzerSystem = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly HealthAnalyzerSystem _healthAnalyzerSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    private EntityQuery<BloodstreamComponent> _bloodstreamQuery;
    private EntityQuery<ItemSlotsComponent> _itemSlotsQuery;
    private EntityQuery<FitsInDispenserComponent> _dispenserQuery;
    private EntityQuery<SolutionContainerManagerComponent> _solutionContainerQuery;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoPodComponent, AtmosDeviceUpdateEvent>(OnCryoPodUpdateAtmosphere);
        SubscribeLocalEvent<CryoPodComponent, GasAnalyzerScanEvent>(OnGasAnalyzed);
        SubscribeLocalEvent<CryoPodComponent, EntRemovedFromContainerMessage>(OnEjected);
        SubscribeLocalEvent<CryoPodComponent, EntInsertedIntoContainerMessage>(OnBodyInserted);
        SubscribeLocalEvent<CryoPodComponent, CryoPodUiMessage>(OnUiMessage);

        _bloodstreamQuery = GetEntityQuery<BloodstreamComponent>();
        _itemSlotsQuery = GetEntityQuery<ItemSlotsComponent>();
        _dispenserQuery = GetEntityQuery<FitsInDispenserComponent>();
        _solutionContainerQuery = GetEntityQuery<SolutionContainerManagerComponent>();

        Subs.BuiEvents<CryoPodComponent>(CryoPodUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBoundUiOpened);
        });
    }

    public override void Update(float frameTime)
    {
        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ActiveCryoPodComponent, CryoPodComponent>();

        while (query.MoveNext(out var uid, out _, out var cryoPod))
        {
            if (curTime >= cryoPod.NextInjectionTime)
            {
                cryoPod.NextInjectionTime += cryoPod.BeakerTransferTime;
                Dirty(uid, cryoPod);
                UpdateInjection((uid, cryoPod));
            }

            if (curTime >= cryoPod.NextUiUpdateTime)
            {
                cryoPod.NextUiUpdateTime += cryoPod.UiUpdateInterval;
                Dirty(uid, cryoPod);
                UpdateUi((uid, cryoPod));
            }
        }
    }

    private void OnCryoPodUpdateAtmosphere(Entity<CryoPodComponent> entity, ref AtmosDeviceUpdateEvent args)
    {
        if (!_nodeContainer.TryGetNode(entity.Owner, entity.Comp.PortName, out PortablePipeNode? portNode))
            return;

        if (!TryComp(entity, out CryoPodAirComponent? cryoPodAir))
            return;

        _atmosphereSystem.React(cryoPodAir.Air, portNode);

        if (portNode.NodeGroup is PipeNet { NodeCount: > 1 } net)
        {
            _gasCanisterSystem.MixContainerWithPipeNet(cryoPodAir.Air, net.Air);
        }
    }

    private void OnGasAnalyzed(Entity<CryoPodComponent> entity, ref GasAnalyzerScanEvent args)
    {
        if (!TryComp(entity, out CryoPodAirComponent? cryoPodAir))
            return;

        args.GasMixtures ??= new List<(string, GasMixture?)>();
        args.GasMixtures.Add((Name(entity.Owner), cryoPodAir.Air));
        // If it's connected to a port, include the port side
        // multiply by volume fraction to make sure to send only the gas inside the analyzed pipe element, not the whole pipe system
        if (_nodeContainer.TryGetNode(entity.Owner, entity.Comp.PortName, out PipeNode? port) && port.Air.Volume != 0f)
        {
            var portAirLocal = port.Air.Clone();
            portAirLocal.Multiply(port.Volume / port.Air.Volume);
            portAirLocal.Volume = port.Volume;
            args.GasMixtures.Add((entity.Comp.PortName, portAirLocal));
        }
    }

    private void OnUiMessage(Entity<CryoPodComponent> cryoPod, ref CryoPodUiMessage msg)
    {
        switch (msg.Type)
        {
            case CryoPodUiMessage.MessageType.EjectPatient:
                TryEjectBody(cryoPod.Owner, msg.Actor, cryoPod.Comp);
                break;
            case CryoPodUiMessage.MessageType.EjectBeaker:
                TryEjectBeaker(cryoPod, msg.Actor);
                break;
            case CryoPodUiMessage.MessageType.Inject:
                TryInject(cryoPod, msg.Quantity.GetValueOrDefault());
                break;
        }
    }

    private void OnBoundUiOpened(Entity<CryoPodComponent> cryoPod, ref BoundUIOpenedEvent args)
    {
        UpdateUi(cryoPod);
    }

    private void OnEjected(Entity<CryoPodComponent> cryoPod, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == CryoPodComponent.BodyContainerName)
        {
            ClearInjectionBuffer(cryoPod);
        }

        UpdateUi(cryoPod);
    }

    private void OnBodyInserted(Entity<CryoPodComponent> cryoPod, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != CryoPodComponent.BodyContainerName)
            return;

        _uiSystem.CloseUi(cryoPod.Owner, CryoPodUiKey.Key, args.Entity);
        ClearInjectionBuffer(cryoPod);
        UpdateUi(cryoPod);
    }

    private void TryEjectBeaker(Entity<CryoPodComponent> cryoPod, EntityUid? user)
    {
        if (_itemSlots.TryEject(cryoPod.Owner, cryoPod.Comp.SolutionContainerName, user, out var beaker)
            && user != null)
        {
            // Eject the beaker to the user's hands if possible.
            _hands.PickupOrDrop(user.Value, beaker.Value);
        }

        UpdateUi(cryoPod);
    }

    private void TryInject(Entity<CryoPodComponent> cryoPod, FixedPoint2 transferAmount)
    {
        var patient = cryoPod.Comp.BodyContainer.ContainedEntity;
        if (patient == null)
            return; // Refuse to inject if there is no patient.

        var beaker = _itemSlots.GetItemOrNull(cryoPod, cryoPod.Comp.SolutionContainerName);

        if (beaker == null
            || !beaker.Value.Valid
            || !_dispenserQuery.TryComp(beaker, out var fitsInDispenserComponent)
            || !_solutionContainerQuery.TryComp(beaker, out var beakerSolutionManager)
            || !_solutionContainerQuery.TryComp(cryoPod, out var podSolutionManager)
            || !_solutionContainer.TryGetFitsInDispenser(
                (beaker.Value, fitsInDispenserComponent, beakerSolutionManager),
                out var beakerSolution,
                out _)
            || !_solutionContainer.TryGetSolution(
                (cryoPod.Owner, podSolutionManager),
                CryoPodComponent.InjectionBufferSolutionName,
                out var injectionSolutionComp,
                out var injectionSolution))
        {
            return;
        }

        // Try to transfer 5u from the beaker to the injection buffer.
        if (injectionSolution.AvailableVolume < 1)
            return;

        var amountToTransfer = FixedPoint2.Min(transferAmount, injectionSolution.AvailableVolume);
        var solution = _solutionContainer.SplitSolution(beakerSolution.Value, amountToTransfer);
        _solutionContainer.TryAddSolution(injectionSolutionComp.Value, solution);

        UpdateUi(cryoPod);
    }

    private void UpdateInjection(Entity<CryoPodComponent> entity)
    {
        var patient = entity.Comp.BodyContainer.ContainedEntity;

        if (patient == null
            || !_solutionContainerQuery.TryComp(entity, out var podSolutionManager)
            || !_solutionContainer.TryGetSolution(
                (entity.Owner, podSolutionManager),
                CryoPodComponent.InjectionBufferSolutionName,
                out var injectingSolution,
                out _)
            || !_bloodstreamQuery.TryComp(patient, out var bloodstream))
        {
            return;
        }

        var solutionToInject = _solutionContainer.SplitSolution(injectingSolution.Value,
                                                                entity.Comp.BeakerTransferAmount);

        if (solutionToInject.Volume > 0)
        {
            _bloodstream.TryAddToChemicals((patient.Value, bloodstream), solutionToInject);
            _reactive.DoEntityReaction(patient.Value, solutionToInject, ReactionMethod.Injection);
        }
    }

    private void ClearInjectionBuffer(Entity<CryoPodComponent> cryoPod)
    {
        if (_solutionContainerQuery.TryComp(cryoPod, out var podSolutionManager)
            && _solutionContainer.TryGetSolution(
                (cryoPod.Owner, podSolutionManager),
                CryoPodComponent.InjectionBufferSolutionName,
                out var injectingSolution,
                out _))
        {
            _solutionContainer.RemoveAllSolution(injectingSolution.Value);
        }
    }

    private void UpdateUi(Entity<CryoPodComponent> entity)
    {
        if (!_uiSystem.IsUiOpen(entity.Owner, CryoPodUiKey.Key)
            || !TryComp(entity, out CryoPodAirComponent? air))
            return;

        var patient = entity.Comp.BodyContainer.ContainedEntity;
        var gasMix = _gasAnalyzerSystem.GenerateGasMixEntry("Cryo pod", air.Air);
        var (beakerCapacity, beaker) = GetBeakerInfo(entity);
        var injecting = GetInjectingReagents(entity);
        var health = _healthAnalyzerSystem.GetHealthAnalyzerUiState(patient);
        health.ScanMode = true;

        _uiSystem.ServerSendUiMessage(
            entity.Owner,
            CryoPodUiKey.Key,
            new CryoPodUserMessage(gasMix, health, beakerCapacity, beaker, injecting)
        );
    }

    private (FixedPoint2? capacity, List<ReagentQuantity>? reagents) GetBeakerInfo(Entity<CryoPodComponent> entity)
    {
        if (!_itemSlotsQuery.TryComp(entity, out var itemSlotsComponent))
            return (null, null);

        var beaker = _itemSlots.GetItemOrNull(
            entity.Owner,
            entity.Comp.SolutionContainerName,
            itemSlotsComponent
        );

        if (beaker == null
            || !beaker.Value.Valid
            || !_dispenserQuery.TryComp(beaker, out var fitsInDispenserComponent)
            || !_solutionContainerQuery.TryComp(beaker, out var solutionContainerManagerComponent)
            || !_solutionContainer.TryGetFitsInDispenser(
                    (beaker.Value, fitsInDispenserComponent, solutionContainerManagerComponent),
                    out var containerSolution,
                    out _))
            return (null, null);

        var capacity = containerSolution.Value.Comp.Solution.MaxVolume;
        var reagents = containerSolution.Value.Comp.Solution.Contents
            .Select(reagent => new ReagentQuantity(reagent.Reagent, reagent.Quantity))
            .ToList();

        return (capacity, reagents);
    }

    private List<ReagentQuantity>? GetInjectingReagents(Entity<CryoPodComponent> entity)
    {
        if (!_solutionContainerQuery.TryComp(entity, out var solutionManager)
            || !_solutionContainer.TryGetSolution(
                (entity.Owner, solutionManager),
                CryoPodComponent.InjectionBufferSolutionName,
                out var injectingSolution,
                out _))
            return null;

        return injectingSolution.Value.Comp.Solution.Contents
            .Select(reagent => new ReagentQuantity(reagent.Reagent, reagent.Quantity))
            .ToList();
    }
}
