using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Climbing.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.MedicalScanner;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Tools.Systems;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
namespace Content.Shared.Medical.Cryogenics;

public abstract partial class SharedCryoPodSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly ClimbSystem _climb = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UI = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;

    private EntityQuery<BloodstreamComponent> _bloodstreamQuery;
    private EntityQuery<ItemSlotsComponent> _itemSlotsQuery;
    private EntityQuery<FitsInDispenserComponent> _dispenserQuery;
    private EntityQuery<SolutionContainerManagerComponent> _solutionContainerQuery;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoPodComponent, CanDropTargetEvent>(OnCryoPodCanDropOn);
        SubscribeLocalEvent<CryoPodComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CryoPodComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CryoPodComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
        SubscribeLocalEvent<CryoPodComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<CryoPodComponent, CryoPodDragFinished>(OnDragFinished);
        SubscribeLocalEvent<CryoPodComponent, CryoPodPryFinished>(OnCryoPodPryFinished);
        SubscribeLocalEvent<CryoPodComponent, DragDropTargetEvent>(HandleDragDropOn);
        SubscribeLocalEvent<CryoPodComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CryoPodComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<CryoPodComponent, ActivatableUIOpenAttemptEvent>(OnActivateUIAttempt);
        SubscribeLocalEvent<CryoPodComponent, EntRemovedFromContainerMessage>(OnEjected);
        SubscribeLocalEvent<CryoPodComponent, EntInsertedIntoContainerMessage>(OnBodyInserted);

        _bloodstreamQuery = GetEntityQuery<BloodstreamComponent>();
        _itemSlotsQuery = GetEntityQuery<ItemSlotsComponent>();
        _dispenserQuery = GetEntityQuery<FitsInDispenserComponent>();
        _solutionContainerQuery = GetEntityQuery<SolutionContainerManagerComponent>();

        InitializeInsideCryoPod();

        Subs.BuiEvents<CryoPodComponent>(CryoPodUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBoundUiOpened);
            subs.Event<CryoPodSimpleUiMessage>(OnSimpleUiMessage);
            subs.Event<CryoPodInjectUiMessage>(OnInjectUiMessage);
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = Timing.CurTime;
        var query = EntityQueryEnumerator<ActiveCryoPodComponent, CryoPodComponent>();

        while (query.MoveNext(out var uid, out _, out var cryoPod))
        {
            if (curTime < cryoPod.NextInjectionTime)
                continue;

            cryoPod.NextInjectionTime += cryoPod.BeakerTransferTime;
            Dirty(uid, cryoPod);
            UpdateInjection((uid, cryoPod));
        }
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

        var solutionToInject =
            _solutionContainer.SplitSolution(injectingSolution.Value, entity.Comp.BeakerTransferAmount);

        if (solutionToInject.Volume > 0)
        {
            _bloodstream.TryAddToBloodstream((patient.Value, bloodstream), solutionToInject);
            _reactive.DoEntityReaction(patient.Value, solutionToInject, ReactionMethod.Injection);
        }
    }

    private void HandleDragDropOn(Entity<CryoPodComponent> ent, ref DragDropTargetEvent args)
    {
        if (ent.Comp.BodyContainer.ContainedEntity != null)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.EntryDelay, new CryoPodDragFinished(), ent, target: args.Dragged, used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = false,
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDragFinished(Entity<CryoPodComponent> ent, ref CryoPodDragFinished args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (InsertBody(ent.Owner, args.Args.Target.Value, ent.Comp))
        {
            _adminLogger.Add(LogType.Action, LogImpact.Medium,
                $"{ToPrettyString(args.User)} inserted {ToPrettyString(args.Args.Target.Value)} into {ToPrettyString(ent.Owner)}");
        }
        args.Handled = true;
    }

    private void OnActivateUIAttempt(Entity<CryoPodComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var containedEntity = ent.Comp.BodyContainer.ContainedEntity;
        if (containedEntity == args.User)
            args.Cancel();
    }

    private void OnInteractUsing(Entity<CryoPodComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !ent.Comp.Locked || ent.Comp.BodyContainer.ContainedEntity == null)
            return;

        args.Handled = _tool.UseTool(args.Used, args.User, ent.Owner, ent.Comp.PryDelay, ent.Comp.UnlockToolQuality, new CryoPodPryFinished());
    }

    private void OnCryoPodPryFinished(EntityUid uid, CryoPodComponent cryoPodComponent, CryoPodPryFinished args)
    {
        if (args.Cancelled)
            return;

        var ejected = EjectBody(uid, cryoPodComponent);
        if (ejected != null)
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ejected.Value)} pried out of {ToPrettyString(uid)} by {ToPrettyString(args.User)}");
    }

    private void OnPowerChanged(Entity<CryoPodComponent> ent, ref PowerChangedEvent args)
    {
        // Needed to avoid adding/removing components on a deleted entity
        if (Terminating(ent))
            return;

        if (args.Powered)
        {
            EnsureComp<ActiveCryoPodComponent>(ent);
            ent.Comp.NextInjectionTime = Timing.CurTime + ent.Comp.BeakerTransferTime;
            Dirty(ent);
        }
        else
        {
            RemComp<ActiveCryoPodComponent>(ent);
            UI.CloseUi(ent.Owner, HealthAnalyzerUiKey.Key);
        }

        UpdateAppearance(ent.Owner, ent.Comp);
    }

    private void OnExamined(Entity<CryoPodComponent> entity, ref ExaminedEvent args)
    {
        var container = _itemSlots.GetItemOrNull(entity.Owner, entity.Comp.SolutionContainerName);
        if (args.IsInDetailsRange && container != null && _solutionContainer.TryGetFitsInDispenser(container.Value, out _, out var containerSolution))
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

    private void OnCryoPodCanDropOn(EntityUid uid, CryoPodComponent component, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.CanDrop = HasComp<BodyComponent>(args.Dragged);
        args.Handled = true;
    }

    private void OnComponentInit(EntityUid uid, CryoPodComponent cryoPodComponent, ComponentInit args)
    {
        cryoPodComponent.BodyContainer = _container.EnsureContainer<ContainerSlot>(uid, CryoPodComponent.BodyContainerName);
    }

    private void UpdateAppearance(EntityUid uid, CryoPodComponent? cryoPod = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref cryoPod))
            return;

        var cryoPodEnabled = HasComp<ActiveCryoPodComponent>(uid);

        if (_light.TryGetLight(uid, out var light))
        {
            _light.SetEnabled(uid, cryoPodEnabled && cryoPod.BodyContainer?.ContainedEntity != null, light);
        }

        if (!Resolve(uid, ref appearance))
            return;

        Appearance.SetData(uid, CryoPodVisuals.ContainsEntity, cryoPod.BodyContainer?.ContainedEntity == null, appearance);
        Appearance.SetData(uid, CryoPodVisuals.IsOn, cryoPodEnabled, appearance);
    }

    public bool InsertBody(EntityUid uid, EntityUid target, CryoPodComponent cryoPodComponent)
    {
        if (cryoPodComponent.BodyContainer.ContainedEntity != null)
            return false;

        if (!HasComp<MobStateComponent>(target))
            return false;

        var xform = Transform(target);
        _container.Insert((target, xform), cryoPodComponent.BodyContainer);

        EnsureComp<InsideCryoPodComponent>(target);
        _standingState.Stand(target, force: true); // Force-stand the mob so that the cryo pod sprite overlays it fully

        UpdateAppearance(uid, cryoPodComponent);
        return true;
    }

    public void TryEjectBody(EntityUid uid, EntityUid userId, CryoPodComponent? cryoPodComponent)
    {
        if (!Resolve(uid, ref cryoPodComponent))
        {
            return;
        }

        if (cryoPodComponent.Locked)
        {
            _popup.PopupClient(Loc.GetString("cryo-pod-locked"), uid, userId);
            return;
        }

        var ejected = EjectBody(uid, cryoPodComponent);
        if (ejected != null)
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ejected.Value)} ejected from {ToPrettyString(uid)} by {ToPrettyString(userId)}");
    }

    /// <summary>
    /// Ejects the contained body
    /// </summary>
    /// <param name="uid">The cryopod entity</param>
    /// <param name="cryoPodComponent">Cryopod component of <see cref="uid"/></param>
    /// <returns>Ejected entity</returns>
    public EntityUid? EjectBody(EntityUid uid, CryoPodComponent? cryoPodComponent)
    {
        if (!Resolve(uid, ref cryoPodComponent))
            return null;

        if (cryoPodComponent.BodyContainer.ContainedEntity is not { Valid: true } contained)
            return null;

        _container.Remove(contained, cryoPodComponent.BodyContainer);
        // InsideCryoPodComponent is removed automatically in its EntGotRemovedFromContainerMessage listener
        // RemComp<InsideCryoPodComponent>(contained);

        // Restore the correct position of the patient. Checking the components manually feels hacky, but I did not find a better way for now.
        if (HasComp<KnockedDownComponent>(contained) || _mobState.IsIncapacitated(contained))
            _standingState.Down(contained);
        else
            _standingState.Stand(contained);

        _climb.ForciblySetClimbing(contained, uid);
        UpdateAppearance(uid, cryoPodComponent);
        return contained;
    }

    public void TryEjectBeaker(Entity<CryoPodComponent> cryoPod, EntityUid? user)
    {
        if (_itemSlots.TryEject(cryoPod.Owner, cryoPod.Comp.SolutionContainerName, user, out var beaker)
            && user != null)
        {
            // Eject the beaker to the user's hands if possible.
            _hands.PickupOrDrop(user.Value, beaker.Value);
        }
    }

    /// <summary>
    /// Transfers reagents from the cryopod beaker into the injection buffer.
    /// </summary>
    /// <param name="cryoPod">The cryopod entity</param>
    /// <param name="transferAmount">
    /// The amount of reagents that will be transferred.
    /// If less reagents are available, however much is available will be transferred.
    /// </param>
    public void TryInject(Entity<CryoPodComponent> cryoPod, FixedPoint2 transferAmount)
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

        if (injectionSolution.AvailableVolume == 0)
            return;

        var amountToTransfer = FixedPoint2.Min(transferAmount, injectionSolution.AvailableVolume);
        var solution = _solutionContainer.SplitSolution(beakerSolution.Value, amountToTransfer);
        _solutionContainer.TryAddSolution(injectionSolutionComp.Value, solution);
    }

    public void ClearInjectionBuffer(Entity<CryoPodComponent> cryoPod)
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

    protected (FixedPoint2? capacity, List<ReagentQuantity>? reagents) GetBeakerInfo(Entity<CryoPodComponent> entity)
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

    protected List<ReagentQuantity>? GetInjectingReagents(Entity<CryoPodComponent> entity)
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

    protected void AddAlternativeVerbs(EntityUid uid, CryoPodComponent cryoPodComponent, GetVerbsEvent<AlternativeVerb> args)
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

    protected void OnEmagged(EntityUid uid, CryoPodComponent? cryoPodComponent, ref GotEmaggedEvent args)
    {
        if (!Resolve(uid, ref cryoPodComponent))
            return;

        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (cryoPodComponent.PermaLocked && cryoPodComponent.Locked)
            return;

        cryoPodComponent.PermaLocked = true;
        cryoPodComponent.Locked = true;
        Dirty(uid, cryoPodComponent);
        args.Handled = true;
    }

    private void OnSimpleUiMessage(Entity<CryoPodComponent> cryoPod, ref CryoPodSimpleUiMessage msg)
    {
        switch (msg.Type)
        {
            case CryoPodSimpleUiMessage.MessageType.EjectPatient:
                TryEjectBody(cryoPod.Owner, msg.Actor, cryoPod.Comp);
                break;
            case CryoPodSimpleUiMessage.MessageType.EjectBeaker:
                TryEjectBeaker(cryoPod, msg.Actor);
                break;
        }

        UpdateUi(cryoPod);
    }

    private void OnInjectUiMessage(Entity<CryoPodComponent> cryoPod, ref CryoPodInjectUiMessage msg)
    {
        TryInject(cryoPod, msg.Quantity);
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
            UI.CloseUi(cryoPod.Owner, CryoPodUiKey.Key, args.Entity);
            ClearInjectionBuffer(cryoPod);
        }

        UpdateUi(cryoPod);
    }

    protected abstract void UpdateUi(Entity<CryoPodComponent> cryoPod);

    [Serializable, NetSerializable]
    public sealed partial class CryoPodPryFinished : SimpleDoAfterEvent;

    [Serializable, NetSerializable]
    public sealed partial class CryoPodDragFinished : SimpleDoAfterEvent;
}
