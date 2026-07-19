using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Server.Medical.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Medical.Cryogenics;
using Robust.Shared.Timing;

namespace Content.Server.Medical;

public sealed partial class CryoPodSystem : SharedCryoPodSystem
{
    [Dependency] private AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private GasCanisterSystem _gasCanisterSystem = default!;
    [Dependency] private GasAnalyzerSystem _gasAnalyzerSystem = default!;
    [Dependency] private HealthAnalyzerSystem _healthAnalyzerSystem = default!;
    [Dependency] private NodeContainerSystem _nodeContainer = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityTimerSystem _timers = default!;

    private static readonly EntityTimerId UiUpdateTimer = new("ui-update");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoPodComponent, AtmosDeviceUpdateEvent>(OnCryoPodUpdateAtmosphere);
        SubscribeLocalEvent<CryoPodComponent, GasAnalyzerScanEvent>(OnGasAnalyzed);
        SubscribeLocalEvent<ActiveCryoPodComponent, ComponentStartup>(OnActiveStartup);
        SubscribeLocalEvent<ActiveCryoPodComponent, EntityTimerEvent>(OnUiUpdateTimer);
    }

    private void OnActiveStartup(Entity<ActiveCryoPodComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp(ent, out CryoPodComponent? cryoPod))
            return;

        _timers.SetTimer(ent, UiUpdateTimer, cryoPod.UiUpdateInterval, cryoPod.UiUpdateInterval);
    }

    private void OnUiUpdateTimer(Entity<ActiveCryoPodComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != UiUpdateTimer || !TryComp(ent, out CryoPodComponent? cryoPod))
            return;

        Dirty(ent.Owner, cryoPod);
        UpdateUi((ent.Owner, cryoPod));
    }

    protected override void UpdateUi(Entity<CryoPodComponent> entity)
    {
        if (!UI.IsUiOpen(entity.Owner, CryoPodUiKey.Key)
            || !TryComp(entity, out CryoPodAirComponent? air))
            return;

        var patient = entity.Comp.BodyContainer.ContainedEntity;
        var gasMix = _gasAnalyzerSystem.GenerateGasMixEntry("Cryo pod", air.Air);
        var (beakerCapacity, beaker) = GetBeakerInfo(entity);
        var injecting = GetInjectingReagents(entity);
        var health = _healthAnalyzerSystem.GetHealthAnalyzerUiState(patient);
        health.ScanMode = true;
        var hasDamage = patient is null ? false : _damageable.GetTotalDamage(patient.Value) > 0;

        UI.ServerSendUiMessage(
            entity.Owner,
            CryoPodUiKey.Key,
            new CryoPodUserMessage(gasMix, health, beakerCapacity, beaker, injecting, hasDamage)
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
}
