using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Server.Medical.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Medical.Cryogenics;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
namespace Content.Server.Medical;

public sealed partial class CryoPodSystem : SharedCryoPodSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly GasCanisterSystem _gasCanisterSystem = default!;
    [Dependency] private readonly GasAnalyzerSystem _gasAnalyzerSystem = default!;
    [Dependency] private readonly HealthAnalyzerSystem _healthAnalyzerSystem = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoPodComponent, AtmosDeviceUpdateEvent>(OnCryoPodUpdateAtmosphere);
        SubscribeLocalEvent<CryoPodComponent, GasAnalyzerScanEvent>(OnGasAnalyzed);
        SubscribeLocalEvent<CryoPodComponent, EntRemovedFromContainerMessage>(OnEjected);
        SubscribeLocalEvent<CryoPodComponent, EntInsertedIntoContainerMessage>(OnBodyInserted);
        SubscribeLocalEvent<CryoPodComponent, CryoPodUiMessage>(OnUiMessage);

        Subs.BuiEvents<CryoPodComponent>(CryoPodUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBoundUiOpened);
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ActiveCryoPodComponent, CryoPodComponent>();

        while (query.MoveNext(out var uid, out _, out var cryoPod))
        {
            if (curTime < cryoPod.NextUiUpdateTime)
                continue;

            cryoPod.NextUiUpdateTime += cryoPod.UiUpdateInterval;
            Dirty(uid, cryoPod);
            UpdateUi((uid, cryoPod));
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

        UpdateUi(cryoPod);
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
        if (args.Container.ID == CryoPodComponent.BodyContainerName)
        {
            _uiSystem.CloseUi(cryoPod.Owner, CryoPodUiKey.Key, args.Entity);
            ClearInjectionBuffer(cryoPod);
        }

        UpdateUi(cryoPod);
    }
}
