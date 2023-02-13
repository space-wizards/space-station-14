using System.Threading;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Climbing;
using Content.Server.DoAfter;
using Content.Server.Medical.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Tools;
using Content.Server.UserInterface;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.DragDrop;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Medical.Cryogenics;
using Content.Shared.MedicalScanner;
using Content.Shared.Tools.Components;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Medical;

public sealed partial class CryoPodSystem: SharedCryoPodSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly GasCanisterSystem _gasCanisterSystem = default!;
    [Dependency] private readonly ClimbSystem _climbSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ToolSystem _toolSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly ReactiveSystem _reactiveSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoPodComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CryoPodComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
        SubscribeLocalEvent<CryoPodComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<CryoPodComponent, DoInsertCryoPodEvent>(DoInsertCryoPod);
        SubscribeLocalEvent<CryoPodComponent, DoInsertCancelledCryoPodEvent>(DoInsertCancelCryoPod);
        SubscribeLocalEvent<CryoPodComponent, CryoPodPryFinished>(OnCryoPodPryFinished);
        SubscribeLocalEvent<CryoPodComponent, CryoPodPryInterrupted>(OnCryoPodPryInterrupted);

        SubscribeLocalEvent<CryoPodComponent, AtmosDeviceUpdateEvent>(OnCryoPodUpdateAtmosphere);
        SubscribeLocalEvent<CryoPodComponent, DragDropEvent>(HandleDragDropOn);
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
        foreach (var (_, cryoPod) in EntityQuery<ActiveCryoPodComponent, CryoPodComponent>())
        {
            metaDataQuery.TryGetComponent(cryoPod.Owner, out var metaDataComponent);
            if (curTime < cryoPod.NextInjectionTime + _metaDataSystem.GetPauseTime(cryoPod.Owner, metaDataComponent))
                continue;
            cryoPod.NextInjectionTime = curTime + TimeSpan.FromSeconds(cryoPod.BeakerTransferTime);

            if (!itemSlotsQuery.TryGetComponent(cryoPod.Owner, out var itemSlotsComponent))
            {
                continue;
            }
            var container = _itemSlotsSystem.GetItemOrNull(cryoPod.Owner, cryoPod.SolutionContainerName, itemSlotsComponent);
            var patient = cryoPod.BodyContainer.ContainedEntity;
            if (container != null
                && container.Value.Valid
                && patient != null
                && fitsInDispenserQuery.TryGetComponent(container, out var fitsInDispenserComponent)
                && solutionContainerManagerQuery.TryGetComponent(container,
                    out var solutionContainerManagerComponent)
                && _solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSolution, dispenserFits: fitsInDispenserComponent, solutionManager: solutionContainerManagerComponent))
            {
                if (!bloodStreamQuery.TryGetComponent(patient, out var bloodstream))
                {
                    continue;
                }

                var solutionToInject = _solutionContainerSystem.SplitSolution(container.Value, containerSolution, cryoPod.BeakerTransferAmount);
                _bloodstreamSystem.TryAddToChemicals(patient.Value, solutionToInject, bloodstream);
                _reactiveSystem.DoEntityReaction(patient.Value, solutionToInject, ReactionMethod.Injection);
            }
        }
    }

    public override void EjectBody(EntityUid uid, SharedCryoPodComponent? cryoPodComponent)
    {
        if (!Resolve(uid, ref cryoPodComponent))
            return;
        if (cryoPodComponent.BodyContainer.ContainedEntity is not {Valid: true} contained)
            return;
        base.EjectBody(uid, cryoPodComponent);
        _climbSystem.ForciblySetClimbing(contained, uid);
    }

    #region Interaction

    private void HandleDragDropOn(EntityUid uid, CryoPodComponent cryoPodComponent, DragDropEvent args)
    {
        if (cryoPodComponent.BodyContainer.ContainedEntity != null)
        {
            return;
        }

        if (cryoPodComponent.DragDropCancelToken != null)
        {
            cryoPodComponent.DragDropCancelToken.Cancel();
            cryoPodComponent.DragDropCancelToken = null;
            return;
        }

        cryoPodComponent.DragDropCancelToken = new CancellationTokenSource();
        var doAfterArgs = new DoAfterEventArgs(args.User, cryoPodComponent.EntryDelay, cryoPodComponent.DragDropCancelToken.Token, uid, args.Dragged)
        {
            BreakOnDamage = true,
            BreakOnStun = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            NeedHand = false,
            TargetFinishedEvent = new DoInsertCryoPodEvent(args.Dragged),
            TargetCancelledEvent = new DoInsertCancelledCryoPodEvent()
        };
        _doAfterSystem.DoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnActivateUIAttempt(EntityUid uid, CryoPodComponent cryoPodComponent, ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
        {
            return;
        }

        var containedEntity = cryoPodComponent.BodyContainer.ContainedEntity;
        if (containedEntity == null || containedEntity == args.User || !HasComp<ActiveCryoPodComponent>(uid))
        {
            args.Cancel();
        }
    }

    private void OnActivateUI(EntityUid uid, CryoPodComponent cryoPodComponent, AfterActivatableUIOpenEvent args)
    {
        _userInterfaceSystem.TrySendUiMessage(
            uid,
            SharedHealthAnalyzerComponent.HealthAnalyzerUiKey.Key,
            new SharedHealthAnalyzerComponent.HealthAnalyzerScannedUserMessage(cryoPodComponent.BodyContainer.ContainedEntity));
    }

    private void OnInteractUsing(EntityUid uid, CryoPodComponent cryoPodComponent, InteractUsingEvent args)
    {
        if (args.Handled || !cryoPodComponent.Locked || cryoPodComponent.BodyContainer.ContainedEntity == null)
            return;

        if (TryComp(args.Used, out ToolComponent? tool)
            && tool.Qualities.Contains("Prying")) // Why aren't those enums?
        {
            if (cryoPodComponent.IsPrying)
                return;
            cryoPodComponent.IsPrying = true;

            _toolSystem.UseTool(args.Used, args.User, uid, 0f,
                cryoPodComponent.PryDelay, "Prying",
                new CryoPodPryFinished(), new CryoPodPryInterrupted(), uid);

            args.Handled = true;
        }
    }

    private void OnExamined(EntityUid uid, CryoPodComponent component, ExaminedEvent args)
    {
        var container = _itemSlotsSystem.GetItemOrNull(component.Owner, component.SolutionContainerName);
        if (args.IsInDetailsRange && container != null && _solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSolution))
        {
            args.PushMarkup(Loc.GetString("cryo-pod-examine", ("beaker", Name(container.Value))));
            if (containerSolution.Volume == 0)
            {
                args.PushMarkup(Loc.GetString("cryo-pod-empty-beaker"));
            }
        }
    }

    private void OnPowerChanged(EntityUid uid, CryoPodComponent component, ref PowerChangedEvent args)
    {
        // Needed to avoid adding/removing components on a deleted entity
        if (Terminating(uid))
        {
            return;
        }

        if (args.Powered)
        {
            EnsureComp<ActiveCryoPodComponent>(uid);
        }
        else
        {
            RemComp<ActiveCryoPodComponent>(uid);
            _uiSystem.TryCloseAll(uid, SharedHealthAnalyzerComponent.HealthAnalyzerUiKey.Key);
        }
        UpdateAppearance(uid, component);
    }

    #endregion

    #region Atmos handler

    private void OnCryoPodUpdateAtmosphere(EntityUid uid, CryoPodComponent cryoPod, AtmosDeviceUpdateEvent args)
    {
        if (!TryComp(uid, out NodeContainerComponent? nodeContainer))
            return;

        if (!nodeContainer.TryGetNode(cryoPod.PortName, out PortablePipeNode? portNode))
            return;
        _atmosphereSystem.React(cryoPod.Air, portNode);

        if (portNode.NodeGroup is PipeNet {NodeCount: > 1} net)
        {
            _gasCanisterSystem.MixContainerWithPipeNet(cryoPod.Air, net.Air);
        }
    }

    private void OnGasAnalyzed(EntityUid uid, CryoPodComponent component, GasAnalyzerScanEvent args)
    {
        var gasMixDict = new Dictionary<string, GasMixture?> { { Name(uid), component.Air } };
        // If it's connected to a port, include the port side
        if (TryComp(uid, out NodeContainerComponent? nodeContainer))
        {
            if(nodeContainer.TryGetNode(component.PortName, out PipeNode? port))
                gasMixDict.Add(component.PortName, port.Air);
        }
        args.GasMixtures = gasMixDict;
    }


    #endregion
}
