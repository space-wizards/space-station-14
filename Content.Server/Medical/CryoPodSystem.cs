using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Climbing;
using Content.Server.DoAfter;
using Content.Server.Medical.Components;
using Content.Server.MobState;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Tools;
using Content.Server.UserInterface;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.DragDrop;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Medical.Cryogenics;
using Content.Shared.MedicalScanner;
using Content.Shared.MobState.Components;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Tools.Components;
using Content.Shared.Verbs;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.Medical;

public sealed class CryoPodSystem: EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly GasCanisterSystem _gasCanisterSystem = default!;
    [Dependency] private readonly ClimbSystem _climbSystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly StandingStateSystem _standingStateSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly ToolSystem _toolSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoPodComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CryoPodComponent, AtmosDeviceUpdateEvent>(OnCryoPodUpdateAtmosphere);
        SubscribeLocalEvent<CryoPodComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
        SubscribeLocalEvent<CryoPodComponent, DragDropEvent>(HandleDragDropOn);
        SubscribeLocalEvent<CryoPodComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CryoPodComponent, DoInsertCryoPodEvent>(DoInsertCryoPod);
        SubscribeLocalEvent<CryoPodComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CryoPodComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<CryoPodComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<CryoPodComponent, CryoPodPryFinished>(OnCryoPodPryFinished);
        SubscribeLocalEvent<CryoPodComponent, CryoPodPryInterrupted>(OnCryoPodPryInterrupted);
        SubscribeLocalEvent<CryoPodComponent, DestructionEventArgs>(OnDestroyed);
        SubscribeLocalEvent<CryoPodComponent, GasAnalyzerScanEvent>(OnGasAnalyzed);
        SubscribeLocalEvent<CryoPodComponent, ActivatableUIOpenAttemptEvent>(OnActivateUIAttempt);
        SubscribeLocalEvent<CryoPodComponent, AfterActivatableUIOpenEvent>(OnActivateUI);
    }

    private void OnComponentInit(EntityUid uid, CryoPodComponent cryoPodComponent, ComponentInit args)
    {
        base.Initialize();
        cryoPodComponent.BodyContainer = _containerSystem.EnsureContainer<ContainerSlot>(uid, $"scanner-bodyContainer");
    }

    private void UpdateAppearance(EntityUid uid, CryoPodComponent? cryoPod = null)
    {
        if (!Resolve(uid, ref cryoPod))
            return;

        var cryoPodEnabled = HasComp<ActiveCryoPodComponent>(uid);
        _appearanceSystem.SetData(uid, SharedCryoPodComponent.CryoPodVisuals.ContainsEntity, cryoPod.BodyContainer.ContainedEntity == null);
        _appearanceSystem.SetData(uid, SharedCryoPodComponent.CryoPodVisuals.IsOn, cryoPodEnabled);
        if (TryComp<PointLightComponent>(uid, out var light))
        {
            light.Enabled = cryoPodEnabled && cryoPod.BodyContainer.ContainedEntity != null;
        }

        _appearanceSystem.SetData(uid,SharedCryoPodComponent.CryoPodVisuals.PanelOpen, false);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var (_, cryoPod) in EntityQuery<ActiveCryoPodComponent, CryoPodComponent>())
        {
            cryoPod.Accumulator += frameTime;

            if (cryoPod.Accumulator < cryoPod.BeakerTransferTime)
                continue;

            cryoPod.Accumulator -= cryoPod.BeakerTransferTime;

            var container = _itemSlotsSystem.GetItemOrNull(cryoPod.Owner, cryoPod.SolutionContainerName);
            var patient = cryoPod.BodyContainer.ContainedEntity;
            if (container != null
                && container.Value.Valid
                && patient != null
                && _solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSolution))
            {
                if (!TryComp<BloodstreamComponent>(patient, out var bloodstream))
                {
                    continue;
                }

                var solutionToInject = _solutionContainerSystem.SplitSolution(container.Value, containerSolution, cryoPod.BeakerTransferAmount);
                _bloodstreamSystem.TryAddToChemicals(patient.Value, solutionToInject, bloodstream);
                solutionToInject.DoEntityReaction(patient.Value, ReactionMethod.Injection);
            }
        }
    }

    public void InsertBody(EntityUid uid, EntityUid target, CryoPodComponent cryoPodComponent)
    {
        if (cryoPodComponent.BodyContainer.ContainedEntity != null)
            return;

        if (!HasComp<MobStateComponent>(target))
            return;

        var xform = Transform(target);
        cryoPodComponent.BodyContainer.Insert(target, transform: xform);

        var comp = EnsureComp<InsideCryoPodComponent>(target);
        comp.Holder = cryoPodComponent.Owner;
        _standingStateSystem.Stand(target, force: true); // Force-stand the mob so that the cryo pod sprite overlays it fully

        UpdateAppearance(uid, cryoPodComponent);
    }

    public void TryEjectBody(EntityUid uid, EntityUid userId, CryoPodComponent? cryoPodComponent)
    {
        if (!Resolve(uid, ref cryoPodComponent))
        {
            return;
        }

        if (cryoPodComponent.Locked)
        {
            _popupSystem.PopupEntity(Loc.GetString("cryo-pod-locked"), uid, Filter.Entities(userId));
            return;
        }

        EjectBody(uid, cryoPodComponent);
    }

    public void EjectBody(EntityUid uid, CryoPodComponent? cryoPodComponent)
    {
        if (!Resolve(uid, ref cryoPodComponent))
            return;

        if (cryoPodComponent.BodyContainer.ContainedEntity is not {Valid: true} contained)
            return;

        cryoPodComponent.BodyContainer.Remove(contained);
        RemComp<InsideCryoPodComponent>(contained);
        _climbSystem.ForciblySetClimbing(contained, uid);

        // Restore the correct position of the patient. Checking the components manually feels hacky, but I did not find a better way for now.
        if (HasComp<KnockedDownComponent>(contained) || _mobStateSystem.IsIncapacitated(contained))
        {
            _standingStateSystem.Down(contained);
        }
        else
        {
            _standingStateSystem.Stand(contained);
        }

        UpdateAppearance(uid, cryoPodComponent);
    }

    #region Interaction

    private void HandleDragDropOn(EntityUid uid, CryoPodComponent cryoPodComponent, DragDropEvent args)
    {
        if (cryoPodComponent.BodyContainer.ContainedEntity != null)
        {
            return;
        }

        var doAfterArgs = new DoAfterEventArgs(args.User, cryoPodComponent.EntryDelay, default, uid, args.Dragged)
        {
            BreakOnDamage = true,
            BreakOnStun = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            NeedHand = false,
            TargetFinishedEvent = new DoInsertCryoPodEvent(args.Dragged),
        };
        _doAfterSystem.DoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void DoInsertCryoPod(EntityUid uid, CryoPodComponent cryoPodComponent, DoInsertCryoPodEvent args)
    {
        InsertBody(uid, args.ToInsert, cryoPodComponent);
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

    private void OnCryoPodPryFinished(EntityUid uid, CryoPodComponent cryoPodComponent, CryoPodPryFinished args)
    {
        cryoPodComponent.IsPrying = false;
        EjectBody(uid, cryoPodComponent);
    }

    private void OnCryoPodPryInterrupted(EntityUid uid, CryoPodComponent cryoPodComponent, CryoPodPryInterrupted args)
    {
        cryoPodComponent.IsPrying = false;
    }

    private void AddAlternativeVerbs(EntityUid uid, CryoPodComponent cryoPodComponent, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Eject verb
        if (cryoPodComponent.BodyContainer.ContainedEntity != null)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("cryo-pod-verb-noun-occupant"),
                Category = VerbCategory.Eject,
                Priority = 1, // Promote to top to make ejecting the ALT-click action
                Act = () => TryEjectBody(uid, args.User, cryoPodComponent)
            });
        }
    }

    private void OnExamined(EntityUid uid, CryoPodComponent component, ExaminedEvent args)
    {
        var container = _itemSlotsSystem.GetItemOrNull(component.Owner, component.SolutionContainerName);
        if (args.IsInDetailsRange && container != null && _solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSolution))
        {
            args.PushMarkup(Loc.GetString("cryo-pod-examine", ("beaker", Name(container.Value))));
            if (containerSolution.CurrentVolume == 0)
            {
                args.PushMarkup(Loc.GetString("cryo-pod-empty-beaker"));
            }
        }
    }

    private void OnDestroyed(EntityUid uid, CryoPodComponent? cryoPodComponent, DestructionEventArgs args)
    {
        if (!Resolve(uid, ref cryoPodComponent))
        {
            return;
        }
        EjectBody(uid, cryoPodComponent);
    }

    private void OnEmagged(EntityUid uid, CryoPodComponent? cryoPodComponent, GotEmaggedEvent args)
    {
        if (!Resolve(uid, ref cryoPodComponent))
        {
            return;
        }

        cryoPodComponent.PermaLocked = true;
        cryoPodComponent.Locked = true;
        args.Handled = true;
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

    #region Event records

    private record DoInsertCryoPodEvent(EntityUid ToInsert);
    private record CryoPodPryFinished;
    private record CryoPodPryInterrupted;

    #endregion
}
