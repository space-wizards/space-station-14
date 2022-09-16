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
        SubscribeLocalEvent<CryoPodComponent, InteractHandEvent>(HandleInteractHand);
        SubscribeLocalEvent<CryoPodComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<DoInsertCryoPodEvent>(DoInsertCryoPod);
        SubscribeLocalEvent<CryoPodComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CryoPodComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<CryoPodComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<CryoPodPryFinished>(OnCryoPodPryFinished);
        SubscribeLocalEvent<CryoPodPryInterrupted>(OnCryoPodPryInterrupted);
        SubscribeLocalEvent<CryoPodComponent, DestructionEventArgs>(OnDestroyed);
    }

    private void OnComponentInit(EntityUid uid, CryoPodComponent cryoPodComponent, ComponentInit args)
    {
        base.Initialize();
        cryoPodComponent.BodyContainer = _containerSystem.EnsureContainer<ContainerSlot>(uid, $"scanner-bodyContainer");
        cryoPodComponent.BodyContainer.ShowContents = true;
    }

    private void UpdateAppearance(EntityUid uid, CryoPodComponent? cryoPod = null)
    {
        if (!Resolve(uid, ref cryoPod))
            return;

        _appearanceSystem.SetData(uid, SharedCryoPodComponent.CryoPodVisuals.IsOpen, cryoPod.BodyContainer.ContainedEntity == null);
        _appearanceSystem.SetData(uid, SharedCryoPodComponent.CryoPodVisuals.IsOn, cryoPod.Enabled);
        if (TryComp<PointLightComponent>(uid, out var light))
        {
            light.Enabled = cryoPod.Enabled && cryoPod.BodyContainer.ContainedEntity != null;
        }

        _appearanceSystem.SetData(uid,SharedCryoPodComponent.CryoPodVisuals.PanelOpen, false);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var cryoPod in EntityQuery<CryoPodComponent>())
        {
            if (!cryoPod.Enabled)
            {
                continue;
            }
            cryoPod.Accumulator += frameTime;

            if (cryoPod.Accumulator < cryoPod.BeakerTransferTime)
                continue;

            cryoPod.Accumulator -= cryoPod.BeakerTransferTime;

            var container = _itemSlotsSystem.GetItem(cryoPod.Owner, "beakerSlot");
            if (container != null
                && container.Value.Valid
                && cryoPod.BodyContainer.ContainedEntity != null
                && TryComp<BloodstreamComponent>(cryoPod.BodyContainer.ContainedEntity, out var bloodstream)
                && _solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSolution))
            {
                var solutionToInject = _solutionContainerSystem.SplitSolution(container.Value, containerSolution, cryoPod.BeakerTransferAmount);
                _bloodstreamSystem.TryAddToChemicals(bloodstream.Owner, solutionToInject, bloodstream);
                solutionToInject.DoEntityReaction(bloodstream.Owner, ReactionMethod.Injection);
            }
        }
    }

    public void InsertBody(EntityUid uid, EntityUid target, CryoPodComponent cryoPodComponent)
    {
        if (cryoPodComponent.BodyContainer.ContainedEntity != null)
            return;

        if (!TryComp<MobStateComponent>(target, out var _comp) || !TryComp<TransformComponent>(target, out var xform))
            return;

        cryoPodComponent.BodyContainer.Insert(target, transform: xform);
        xform.LocalPosition = new Vector2(0, 1); // So that the target appears to be floating within the pod

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
        if (TryComp<KnockedDownComponent>(contained, out var _knockedDown)
            || TryComp<MobStateComponent>(contained, out var mobStateComponent) && _mobStateSystem.IsIncapacitated(contained))
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

        var doAfterArgs = new DoAfterEventArgs(args.User, cryoPodComponent.EntryDelay, default, args.Dragged)
        {
            BreakOnDamage = true,
            BreakOnStun = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            NeedHand = false,
            BroadcastFinishedEvent = new DoInsertCryoPodEvent(cryoPodComponent, args.Dragged, uid),
        };
        _doAfterSystem.DoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void DoInsertCryoPod(DoInsertCryoPodEvent ev)
    {
        InsertBody(ev.Unit, ev.ToInsert, ev.CryoPod);
    }

    private void HandleInteractHand(EntityUid uid, CryoPodComponent cryoPodComponent, InteractHandEvent args)
    {
        if (!cryoPodComponent.Enabled)
        {
            return;
        }
        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;

        if (cryoPodComponent.BodyContainer.ContainedEntity != null)
        {
            if (args.User == cryoPodComponent.BodyContainer.ContainedEntity)
            {
                return;
            }
            _userInterfaceSystem.TryOpen(uid, SharedHealthAnalyzerComponent.HealthAnalyzerUiKey.Key, actor.PlayerSession);
            _userInterfaceSystem.TrySendUiMessage(
                uid,
                SharedHealthAnalyzerComponent.HealthAnalyzerUiKey.Key,
                new SharedHealthAnalyzerComponent.HealthAnalyzerScannedUserMessage(cryoPodComponent.BodyContainer.ContainedEntity));
            args.Handled = true;
        }
    }

    private void OnInteractUsing(EntityUid uid, CryoPodComponent cryoPodComponent, InteractUsingEvent args)
    {
        if (args.Handled || !cryoPodComponent.Locked || cryoPodComponent.BodyContainer.ContainedEntity == null)
            return;

        if (EntityManager.TryGetComponent(args.Used, out ToolComponent? tool)
            && tool.Qualities.Contains("Prying")) // Why aren't those enums?
        {
            if (cryoPodComponent.IsPrying)
                return;
            cryoPodComponent.IsPrying = true;

            _toolSystem.UseTool(args.Used, args.User, uid, 0f,
                cryoPodComponent.PryDelay, "Prying",
                new CryoPodPryFinished(uid, cryoPodComponent), new CryoPodPryInterrupted(uid, cryoPodComponent));

            args.Handled = true;
        }
    }

    private void OnCryoPodPryFinished(CryoPodPryFinished ev)
    {
        ev.CryoPodComponent.IsPrying = false;
        EjectBody(ev.Uid, ev.CryoPodComponent);
    }

    private void OnCryoPodPryInterrupted(CryoPodPryInterrupted ev)
    {
        ev.CryoPodComponent.IsPrying = false;
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
        var container = _itemSlotsSystem.GetItem(component.Owner, "beakerSlot");
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

    private void OnPowerChanged(EntityUid uid, CryoPodComponent component, PowerChangedEvent args)
    {
        component.Enabled = args.Powered;
        if (!args.Powered)
        {
            _uiSystem.TryCloseAll(uid, SharedHealthAnalyzerComponent.HealthAnalyzerUiKey.Key);
        }
        UpdateAppearance(uid, component);
    }

    #endregion

    #region Atmos handler

    private void OnCryoPodUpdateAtmosphere(EntityUid uid, CryoPodComponent cryoPod, AtmosDeviceUpdateEvent args)
    {
        if (!EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
            return;

        if (!nodeContainer.TryGetNode(cryoPod.PortName, out PortablePipeNode? portNode))
            return;
        _atmosphereSystem.React(cryoPod.Air, portNode);

        if (portNode.NodeGroup is PipeNet {NodeCount: > 1} net)
        {
            _gasCanisterSystem.MixContainerWithPipeNet(cryoPod.Air, net.Air);
        }
    }

    #endregion

    #region Event records

    private record DoInsertCryoPodEvent(CryoPodComponent CryoPod, EntityUid ToInsert, EntityUid Unit);
    private record CryoPodPryFinished(EntityUid Uid, CryoPodComponent CryoPodComponent);
    private record CryoPodPryInterrupted(EntityUid Uid, CryoPodComponent CryoPodComponent);

    #endregion
}
