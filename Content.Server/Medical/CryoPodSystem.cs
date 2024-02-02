using Content.Server.Administration.Logs;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Medical.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Temperature.Components;
using Content.Shared.UserInterface;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Climbing.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Medical.Cryogenics;
using Content.Shared.MedicalScanner;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using SharedToolSystem = Content.Shared.Tools.Systems.SharedToolSystem;

namespace Content.Server.Medical;

public sealed partial class CryoPodSystem : SharedCryoPodSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly GasCanisterSystem _gasCanisterSystem = default!;
    [Dependency] private readonly ClimbSystem _climbSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly ReactiveSystem _reactiveSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoPodComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CryoPodComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
        SubscribeLocalEvent<CryoPodComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<CryoPodComponent, CryoPodDragFinished>(OnDragFinished);
        SubscribeLocalEvent<CryoPodComponent, CryoPodPryFinished>(OnCryoPodPryFinished);

        SubscribeLocalEvent<CryoPodComponent, AtmosDeviceUpdateEvent>(OnCryoPodUpdateAtmosphere);
        SubscribeLocalEvent<CryoPodComponent, DragDropTargetEvent>(HandleDragDropOn);
        SubscribeLocalEvent<CryoPodComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CryoPodComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CryoPodComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<CryoPodComponent, GasAnalyzerScanEvent>(OnGasAnalyzed);
        SubscribeLocalEvent<CryoPodComponent, ActivatableUIOpenAttemptEvent>(OnActivateUIAttempt);
        SubscribeLocalEvent<CryoPodComponent, AfterActivatableUIOpenEvent>(OnActivateUI);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;
        var bloodStreamQuery = GetEntityQuery<BloodstreamComponent>();
        var metaDataQuery = GetEntityQuery<MetaDataComponent>();
        var itemSlotsQuery = GetEntityQuery<ItemSlotsComponent>();
        var fitsInDispenserQuery = GetEntityQuery<FitsInDispenserComponent>();
        var solutionContainerManagerQuery = GetEntityQuery<SolutionContainerManagerComponent>();
        var query = EntityQueryEnumerator<ActiveCryoPodComponent, CryoPodComponent>();

        while (query.MoveNext(out var uid, out _, out var cryoPod))
        {
            metaDataQuery.TryGetComponent(uid, out var metaDataComponent);
            if (curTime < cryoPod.NextInjectionTime + _metaDataSystem.GetPauseTime(uid, metaDataComponent))
                continue;
            cryoPod.NextInjectionTime = curTime + TimeSpan.FromSeconds(cryoPod.BeakerTransferTime);

            if (!itemSlotsQuery.TryGetComponent(uid, out var itemSlotsComponent))
            {
                continue;
            }
            var container = _itemSlotsSystem.GetItemOrNull(uid, cryoPod.SolutionContainerName, itemSlotsComponent);
            var patient = cryoPod.BodyContainer.ContainedEntity;
            if (container != null
                && container.Value.Valid
                && patient != null
                && fitsInDispenserQuery.TryGetComponent(container, out var fitsInDispenserComponent)
                && solutionContainerManagerQuery.TryGetComponent(container,
                    out var solutionContainerManagerComponent)
                && _solutionContainerSystem.TryGetFitsInDispenser((container.Value, fitsInDispenserComponent, solutionContainerManagerComponent),
                    out var containerSolution, out _))
            {
                if (!bloodStreamQuery.TryGetComponent(patient, out var bloodstream))
                {
                    continue;
                }

                var solutionToInject = _solutionContainerSystem.SplitSolution(containerSolution.Value, cryoPod.BeakerTransferAmount);
                _bloodstreamSystem.TryAddToChemicals(patient.Value, solutionToInject, bloodstream);
                _reactiveSystem.DoEntityReaction(patient.Value, solutionToInject, ReactionMethod.Injection);
            }
        }
    }

    public override EntityUid? EjectBody(EntityUid uid, CryoPodComponent? cryoPodComponent)
    {
        if (!Resolve(uid, ref cryoPodComponent))
            return null;
        if (cryoPodComponent.BodyContainer.ContainedEntity is not { Valid: true } contained)
            return null;
        base.EjectBody(uid, cryoPodComponent);
        _climbSystem.ForciblySetClimbing(contained, uid);
        return contained;
    }

    #region Interaction

    private void HandleDragDropOn(Entity<CryoPodComponent> entity, ref DragDropTargetEvent args)
    {
        if (entity.Comp.BodyContainer.ContainedEntity != null)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, entity.Comp.EntryDelay, new CryoPodDragFinished(), entity, target: args.Dragged, used: entity)
        {
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            NeedHand = false,
        };
        _doAfterSystem.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDragFinished(Entity<CryoPodComponent> entity, ref CryoPodDragFinished args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (InsertBody(entity.Owner, args.Args.Target.Value, entity.Comp))
        {
            if (!TryComp(entity.Owner, out CryoPodAirComponent? cryoPodAir))
                _adminLogger.Add(LogType.Action, LogImpact.Medium,
                    $"{ToPrettyString(args.User)} inserted {ToPrettyString(args.Args.Target.Value)} into {ToPrettyString(entity.Owner)}");

            _adminLogger.Add(LogType.Action, LogImpact.Medium,
                $"{ToPrettyString(args.User)} inserted {ToPrettyString(args.Args.Target.Value)} into {ToPrettyString(entity.Owner)} which contains gas: {cryoPodAir!.Air.ToPrettyString():gasMix}");
        }
        args.Handled = true;
    }

    private void OnActivateUIAttempt(Entity<CryoPodComponent> entity, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
        {
            return;
        }

        var containedEntity = entity.Comp.BodyContainer.ContainedEntity;
        if (containedEntity == null || containedEntity == args.User || !HasComp<ActiveCryoPodComponent>(entity))
        {
            args.Cancel();
        }
    }

    private void OnActivateUI(Entity<CryoPodComponent> entity, ref AfterActivatableUIOpenEvent args)
    {
        if (!entity.Comp.BodyContainer.ContainedEntity.HasValue)
            return;

        TryComp<TemperatureComponent>(entity.Comp.BodyContainer.ContainedEntity, out var temp);
        TryComp<BloodstreamComponent>(entity.Comp.BodyContainer.ContainedEntity, out var bloodstream);

        _userInterfaceSystem.TrySendUiMessage(
            entity.Owner,
            HealthAnalyzerUiKey.Key,
            new HealthAnalyzerScannedUserMessage(GetNetEntity(entity.Comp.BodyContainer.ContainedEntity),
            temp?.CurrentTemperature ?? 0,
            (bloodstream != null && _solutionContainerSystem.ResolveSolution(entity.Comp.BodyContainer.ContainedEntity.Value,
                bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution))
                ? bloodSolution.FillFraction
                : 0
        ));
    }

    private void OnInteractUsing(Entity<CryoPodComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled || !entity.Comp.Locked || entity.Comp.BodyContainer.ContainedEntity == null)
            return;

        args.Handled = _toolSystem.UseTool(args.Used, args.User, entity.Owner, entity.Comp.PryDelay, "Prying", new CryoPodPryFinished());
    }

    private void OnExamined(Entity<CryoPodComponent> entity, ref ExaminedEvent args)
    {
        var container = _itemSlotsSystem.GetItemOrNull(entity.Owner, entity.Comp.SolutionContainerName);
        if (args.IsInDetailsRange && container != null && _solutionContainerSystem.TryGetFitsInDispenser(container.Value, out _, out var containerSolution))
        {
            using (args.PushGroup(nameof(CryoPodComponent)))
            {
                args.PushMarkup(Loc.GetString("cryo-pod-examine", ("beaker", Name(container.Value))));
                if (containerSolution.Volume == 0)
                {
                    args.PushMarkup(Loc.GetString("cryo-pod-empty-beaker"));
                }
            }
        }
    }

    private void OnPowerChanged(Entity<CryoPodComponent> entity, ref PowerChangedEvent args)
    {
        // Needed to avoid adding/removing components on a deleted entity
        if (Terminating(entity))
        {
            return;
        }

        if (args.Powered)
        {
            EnsureComp<ActiveCryoPodComponent>(entity);
        }
        else
        {
            RemComp<ActiveCryoPodComponent>(entity);
            _uiSystem.TryCloseAll(entity.Owner, HealthAnalyzerUiKey.Key);
        }
        UpdateAppearance(entity.Owner, entity.Comp);
    }

    #endregion

    #region Atmos handler

    private void OnCryoPodUpdateAtmosphere(Entity<CryoPodComponent> entity, ref AtmosDeviceUpdateEvent args)
    {
        if (!TryComp(entity, out NodeContainerComponent? nodeContainer))
            return;

        if (!_nodeContainer.TryGetNode(nodeContainer, entity.Comp.PortName, out PortablePipeNode? portNode))
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

        var gasMixDict = new Dictionary<string, GasMixture?> { { Name(entity.Owner), cryoPodAir.Air } };
        // If it's connected to a port, include the port side
        if (TryComp(entity, out NodeContainerComponent? nodeContainer))
        {
            if (_nodeContainer.TryGetNode(nodeContainer, entity.Comp.PortName, out PipeNode? port))
                gasMixDict.Add(entity.Comp.PortName, port.Air);
        }
        args.GasMixtures = gasMixDict;
    }

    #endregion
}
