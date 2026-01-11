using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Climbing.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
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
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ClimbSystem _climb = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;

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

        _bloodstreamQuery = GetEntityQuery<BloodstreamComponent>();
        _itemSlotsQuery = GetEntityQuery<ItemSlotsComponent>();
        _dispenserQuery = GetEntityQuery<FitsInDispenserComponent>();
        _solutionContainerQuery = GetEntityQuery<SolutionContainerManagerComponent>();

        InitializeInsideCryoPod();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ActiveCryoPodComponent, CryoPodComponent>();

        while (query.MoveNext(out var uid, out _, out var cryoPod))
        {
            if (curTime < cryoPod.NextInjectionTime)
                continue;

            cryoPod.NextInjectionTime += cryoPod.BeakerTransferTime;
            Dirty(uid, cryoPod);

            if (!_itemSlotsQuery.TryComp(uid, out var itemSlotsComponent))
                continue;

            var container = _itemSlots.GetItemOrNull(uid, cryoPod.SolutionContainerName, itemSlotsComponent);
            var patient = cryoPod.BodyContainer.ContainedEntity;
            if (container != null
                && container.Value.Valid
                && patient != null
                && _dispenserQuery.TryComp(container, out var fitsInDispenserComponent)
                && _solutionContainerQuery.TryComp(container, out var solutionContainerManagerComponent)
                && _solutionContainer.TryGetFitsInDispenser((container.Value, fitsInDispenserComponent, solutionContainerManagerComponent),
                    out var containerSolution, out _)
                && _bloodstreamQuery.TryComp(patient, out var bloodstream))
            {
                var solutionToInject = _solutionContainer.SplitSolution(containerSolution.Value, cryoPod.BeakerTransferAmount);
                _bloodstream.TryAddToBloodstream((patient.Value, bloodstream), solutionToInject);
                _reactive.DoEntityReaction(patient.Value, solutionToInject, ReactionMethod.Injection);
            }
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
        if (containedEntity == null || containedEntity == args.User || !HasComp<ActiveCryoPodComponent>(ent))
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
            ent.Comp.NextInjectionTime = _timing.CurTime + ent.Comp.BeakerTransferTime;
            Dirty(ent);
        }
        else
        {
            RemComp<ActiveCryoPodComponent>(ent);
            _ui.CloseUi(ent.Owner, HealthAnalyzerUiKey.Key);
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

        _appearance.SetData(uid, CryoPodVisuals.ContainsEntity, cryoPod.BodyContainer?.ContainedEntity == null, appearance);
        _appearance.SetData(uid, CryoPodVisuals.IsOn, cryoPodEnabled, appearance);
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

    [Serializable, NetSerializable]
    public sealed partial class CryoPodPryFinished : SimpleDoAfterEvent;

    [Serializable, NetSerializable]
    public sealed partial class CryoPodDragFinished : SimpleDoAfterEvent;
}
