using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Climbing.Systems;
using Content.Shared.Containers;
using Content.Shared.Database;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Unit.Events;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Emag.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Storage.Components;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Disposal.Unit;

[Serializable, NetSerializable]
public sealed partial class DisposalDoAfterEvent : SimpleDoAfterEvent
{
}

public abstract class SharedDisposalUnitSystem : EntitySystem
{
    [Dependency] protected readonly ActionBlockerSystem ActionBlockerSystem = default!;
    [Dependency] private   readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] protected readonly MetaDataSystem Metadata = default!;
    [Dependency] private   readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] private   readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private   readonly ClimbSystem _climb = default!;
    [Dependency] protected readonly SharedContainerSystem Containers = default!;
    [Dependency] protected readonly SharedJointSystem Joints = default!;
    [Dependency] private   readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private   readonly SharedDisposalTubeSystem _disposalTubeSystem = default!;
    [Dependency] private   readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private   readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private   readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] private   readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private   readonly SharedMapSystem _map = default!;

    protected static TimeSpan ExitAttemptDelay = TimeSpan.FromSeconds(0.5);

    // Percentage
    public const float PressurePerSecond = 0.05f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalUnitComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<DisposalUnitComponent, CanDropTargetEvent>(OnCanDragDropOn);
        SubscribeLocalEvent<DisposalUnitComponent, GetVerbsEvent<InteractionVerb>>(AddInsertVerb);
        SubscribeLocalEvent<DisposalUnitComponent, GetVerbsEvent<AlternativeVerb>>(AddDisposalAltVerbs);
        SubscribeLocalEvent<DisposalUnitComponent, GetVerbsEvent<Verb>>(AddClimbInsideVerb);

        SubscribeLocalEvent<DisposalUnitComponent, DisposalDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<DisposalUnitComponent, BeforeThrowInsertEvent>(OnThrowInsert);

        SubscribeLocalEvent<DisposalUnitComponent, DisposalUnitComponent.UiButtonPressedMessage>(OnUiButtonPressed);

        SubscribeLocalEvent<DisposalUnitComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<DisposalUnitComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<DisposalUnitComponent, PowerChangedEvent>(OnPowerChange);
        SubscribeLocalEvent<DisposalUnitComponent, ComponentInit>(OnDisposalInit);

        SubscribeLocalEvent<DisposalUnitComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<DisposalUnitComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<DisposalUnitComponent, DragDropTargetEvent>(OnDragDropOn);
        SubscribeLocalEvent<DisposalUnitComponent, ContainerRelayMovementEntityEvent>(OnMovement);

        SubscribeLocalEvent<DisposalUnitComponent, GetDumpableVerbEvent>(OnGetDumpableVerb);
        SubscribeLocalEvent<DisposalUnitComponent, DumpEvent>(OnDump);
    }

    private void AddDisposalAltVerbs(Entity<DisposalUnitComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var uid = ent.Owner;
        var component = ent.Comp;

        // Behavior for if the disposals bin has items in it
        if (component.Container.ContainedEntities.Count > 0)
        {
            // Verbs to flush the unit
            AlternativeVerb flushVerb = new()
            {
                Act = () => ManualEngage(uid, component),
                Text = Loc.GetString("disposal-flush-verb-get-data-text"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/delete_transparent.svg.192dpi.png")),
                Priority = 1,
            };
            args.Verbs.Add(flushVerb);

            // Verb to eject the contents
            AlternativeVerb ejectVerb = new()
            {
                Act = () => TryEjectContents(uid, component),
                Category = VerbCategory.Eject,
                Text = Loc.GetString("disposal-eject-verb-get-data-text")
            };
            args.Verbs.Add(ejectVerb);
        }
    }

    private void AddInsertVerb(EntityUid uid, DisposalUnitComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || args.Using == null)
            return;

        if (!ActionBlockerSystem.CanDrop(args.User))
            return;

        if (!CanInsert(uid, component, args.Using.Value))
            return;

        InteractionVerb insertVerb = new()
        {
            Text = Name(args.Using.Value),
            Category = VerbCategory.Insert,
            Act = () =>
            {
                _handsSystem.TryDropIntoContainer((args.User, args.Hands), args.Using.Value, component.Container, checkActionBlocker: false);
                _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User):player} inserted {ToPrettyString(args.Using.Value)} into {ToPrettyString(uid)}");
                AfterInsert(uid, component, args.Using.Value, args.User);
            }
        };

        args.Verbs.Add(insertVerb);
    }

    private void OnDoAfter(EntityUid uid, DisposalUnitComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null || args.Args.Used == null)
            return;

        AfterInsert(uid, component, args.Args.Target.Value, args.Args.User, doInsert: true);

        args.Handled = true;
    }

    private void OnThrowInsert(Entity<DisposalUnitComponent> ent, ref BeforeThrowInsertEvent args)
    {
        if (!CanInsert(ent, ent, args.ThrownEntity))
            args.Cancelled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DisposalUnitComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var unit, out var metadata))
        {
            Update(uid, unit, metadata);
        }
    }

    // TODO: This should just use the same thing as entity storage?
    private void OnMovement(EntityUid uid, DisposalUnitComponent component, ref ContainerRelayMovementEntityEvent args)
    {
        var currentTime = GameTiming.CurTime;

        if (!ActionBlockerSystem.CanMove(args.Entity))
            return;

        if (!TryComp(args.Entity, out HandsComponent? hands) ||
            hands.Count == 0 ||
            currentTime < component.LastExitAttempt + ExitAttemptDelay)
            return;

        Dirty(uid, component);
        component.LastExitAttempt = currentTime;
        Remove(uid, component, args.Entity);
        UpdateUI((uid, component));
    }

    private void OnActivate(EntityUid uid, DisposalUnitComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        args.Handled = true;
        _ui.TryToggleUi(uid, DisposalUnitComponent.DisposalUnitUiKey.Key, args.User);
    }

    private void OnAfterInteractUsing(EntityUid uid, DisposalUnitComponent component, AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (!HasComp<HandsComponent>(args.User))
        {
            return;
        }

        if (!CanInsert(uid, component, args.Used) || !_handsSystem.TryDropIntoContainer(args.User, args.Used, component.Container))
        {
            return;
        }

        _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User):player} inserted {ToPrettyString(args.Used)} into {ToPrettyString(uid)}");
        AfterInsert(uid, component, args.Used, args.User);
        args.Handled = true;
    }

    protected virtual void OnDisposalInit(Entity<DisposalUnitComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Container = Containers.EnsureContainer<Container>(ent, DisposalUnitComponent.ContainerId);
    }

    private void OnPowerChange(EntityUid uid, DisposalUnitComponent component, ref PowerChangedEvent args)
    {
        if (!component.Running)
            return;

        UpdateUI((uid, component));
        UpdateVisualState(uid, component);

        if (!args.Powered)
        {
            component.NextFlush = null;
            Dirty(uid, component);
            return;
        }

        if (component.Engaged)
        {
            // Run ManualEngage to recalculate a new flush time
            ManualEngage(uid, component);
        }
    }

    private void OnAnchorChanged(EntityUid uid, DisposalUnitComponent component, ref AnchorStateChangedEvent args)
    {
        if (Terminating(uid))
            return;

        UpdateVisualState(uid, component);
        if (!args.Anchored)
            TryEjectContents(uid, component);
    }

    private void OnDragDropOn(EntityUid uid, DisposalUnitComponent component, ref DragDropTargetEvent args)
    {
        args.Handled = TryInsert(uid, args.Dragged, args.User);
    }

    protected virtual void UpdateUI(Entity<DisposalUnitComponent> entity)
    {

    }

    /// <summary>
    /// Returns the estimated time when the disposal unit will be back to full pressure.
    /// </summary>
    public TimeSpan EstimatedFullPressure(EntityUid uid, DisposalUnitComponent component)
    {
        if (component.NextPressurized < GameTiming.CurTime)
            return TimeSpan.Zero;

        return component.NextPressurized;
    }

    public bool CanFlush(EntityUid unit, DisposalUnitComponent component)
    {
        return GetState(unit, component) == DisposalsPressureState.Ready
               && _power.IsPowered(unit)
               && Comp<TransformComponent>(unit).Anchored;
    }

    public void Remove(EntityUid uid, DisposalUnitComponent component, EntityUid toRemove)
    {
        if (GameTiming.ApplyingState)
            return;

        if (!Containers.Remove(toRemove, component.Container))
            return;

        if (component.Container.ContainedEntities.Count == 0)
        {
            // If not manually engaged then reset the flushing entirely.
            if (!component.Engaged)
            {
                component.NextFlush = null;
                Dirty(uid, component);
                UpdateUI((uid, component));
            }
        }

        _climb.Climb(toRemove, toRemove, uid, silent: true);

        UpdateVisualState(uid, component);
    }

    public void UpdateVisualState(EntityUid uid, DisposalUnitComponent component, bool flush = false)
    {
        if (!TryComp(uid, out AppearanceComponent? appearance))
        {
            return;
        }

        if (!Transform(uid).Anchored)
        {
            _appearance.SetData(uid, DisposalUnitComponent.Visuals.VisualState, DisposalUnitComponent.VisualState.UnAnchored, appearance);
            _appearance.SetData(uid, DisposalUnitComponent.Visuals.Handle, DisposalUnitComponent.HandleState.Normal, appearance);
            _appearance.SetData(uid, DisposalUnitComponent.Visuals.Light, DisposalUnitComponent.LightStates.Off, appearance);
            return;
        }

        var state = GetState(uid, component);

        switch (state)
        {
            case DisposalsPressureState.Flushed:
                _appearance.SetData(uid, DisposalUnitComponent.Visuals.VisualState, DisposalUnitComponent.VisualState.OverlayFlushing, appearance);
                break;
            case DisposalsPressureState.Pressurizing:
                _appearance.SetData(uid, DisposalUnitComponent.Visuals.VisualState, DisposalUnitComponent.VisualState.OverlayCharging, appearance);
                break;
            case DisposalsPressureState.Ready:
                _appearance.SetData(uid, DisposalUnitComponent.Visuals.VisualState, DisposalUnitComponent.VisualState.Anchored, appearance);
                break;
        }

        _appearance.SetData(uid, DisposalUnitComponent.Visuals.Handle, component.Engaged
            ? DisposalUnitComponent.HandleState.Engaged
            : DisposalUnitComponent.HandleState.Normal, appearance);

        if (!_power.IsPowered(uid))
        {
            _appearance.SetData(uid, DisposalUnitComponent.Visuals.Light, DisposalUnitComponent.LightStates.Off, appearance);
            return;
        }

        var lightState = DisposalUnitComponent.LightStates.Off;

        if (component.Container.ContainedEntities.Count > 0)
        {
            lightState |= DisposalUnitComponent.LightStates.Full;
        }

        if (state is DisposalsPressureState.Pressurizing or DisposalsPressureState.Flushed)
        {
            lightState |= DisposalUnitComponent.LightStates.Charging;
        }
        else
        {
            lightState |= DisposalUnitComponent.LightStates.Ready;
        }

        _appearance.SetData(uid, DisposalUnitComponent.Visuals.Light, lightState, appearance);
    }

    /// <summary>
    /// Gets the current pressure state of a disposals unit.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="metadata"></param>
    /// <returns></returns>
    public DisposalsPressureState GetState(EntityUid uid, DisposalUnitComponent component, MetaDataComponent? metadata = null)
    {
        var nextPressure = Metadata.GetPauseTime(uid, metadata) + component.NextPressurized - GameTiming.CurTime;
        var pressurizeTime = 1f / PressurePerSecond;
        var pressurizeDuration = pressurizeTime - component.FlushDelay.TotalSeconds;

        if (nextPressure.TotalSeconds > pressurizeDuration)
        {
            return DisposalsPressureState.Flushed;
        }

        if (nextPressure > TimeSpan.Zero)
        {
            return DisposalsPressureState.Pressurizing;
        }

        return DisposalsPressureState.Ready;
    }

    public float GetPressure(EntityUid uid, DisposalUnitComponent component, MetaDataComponent? metadata = null)
    {
        if (!Resolve(uid, ref metadata))
            return 0f;

        var pauseTime = Metadata.GetPauseTime(uid, metadata);
        return MathF.Min(1f,
            (float)(GameTiming.CurTime - pauseTime - component.NextPressurized).TotalSeconds / PressurePerSecond);
    }

    protected void OnPreventCollide(EntityUid uid, DisposalUnitComponent component,
        ref PreventCollideEvent args)
    {
        var otherBody = args.OtherEntity;

        // Items dropped shouldn't collide but items thrown should
        if (HasComp<ItemComponent>(otherBody) && !HasComp<ThrownItemComponent>(otherBody))
        {
            args.Cancelled = true;
        }
    }

    protected void OnCanDragDropOn(EntityUid uid, DisposalUnitComponent component, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.CanDrop = CanInsert(uid, component, args.Dragged);
        args.Handled = true;
    }

    protected void OnEmagged(EntityUid uid, DisposalUnitComponent component, ref GotEmaggedEvent args)
    {
        component.DisablePressure = true;
        args.Handled = true;
    }

    public virtual bool CanInsert(EntityUid uid, DisposalUnitComponent component, EntityUid entity)
    {
        // TODO: All of the below should be using the EXISTING EVENT
        if (!Containers.CanInsert(entity, component.Container))
            return false;

        if (!Transform(uid).Anchored)
            return false;

        var storable = HasComp<ItemComponent>(entity);
        if (!storable && !HasComp<MobStateComponent>(entity))
            return false;

        if (_whitelistSystem.IsBlacklistPass(component.Blacklist, entity) ||
            _whitelistSystem.IsWhitelistFail(component.Whitelist, entity))
            return false;

        if (TryComp<PhysicsComponent>(entity, out var physics) && (physics.CanCollide) || storable)
            return true;
        else
            return false;
    }

    public void DoInsertDisposalUnit(EntityUid uid,
        EntityUid toInsert,
        EntityUid user,
        DisposalUnitComponent? disposal = null)
    {
        if (!Resolve(uid, ref disposal))
            return;

        if (!Containers.Insert(toInsert, disposal.Container))
            return;

        _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user):player} inserted {ToPrettyString(toInsert)} into {ToPrettyString(uid)}");
        AfterInsert(uid, disposal, toInsert, user);
    }

    public virtual void AfterInsert(EntityUid uid,
        DisposalUnitComponent component,
        EntityUid inserted,
        EntityUid? user = null,
        bool doInsert = false)
    {
        Audio.PlayPredicted(component.InsertSound, uid, user: user);
        if (doInsert && !Containers.Insert(inserted, component.Container))
            return;

        if (user != inserted && user != null)
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user.Value):player} inserted {ToPrettyString(inserted)} into {ToPrettyString(uid)}");

        QueueAutomaticEngage(uid, component);

        _ui.CloseUi(uid, DisposalUnitComponent.DisposalUnitUiKey.Key, inserted);

        // Maybe do pullable instead? Eh still fine.
        Joints.RecursiveClearJoints(inserted);
        UpdateVisualState(uid, component);
    }

    public bool TryInsert(EntityUid unitId, EntityUid toInsertId, EntityUid? userId, DisposalUnitComponent? unit = null)
    {
        if (!Resolve(unitId, ref unit))
            return false;

        if (userId.HasValue && !HasComp<HandsComponent>(userId) && toInsertId != userId) // Mobs like mouse can Jump inside even with no hands
        {
            _popupSystem.PopupEntity(Loc.GetString("disposal-unit-no-hands"), userId.Value, userId.Value, PopupType.SmallCaution);
            return false;
        }

        if (!CanInsert(unitId, unit, toInsertId))
            return false;

        bool insertingSelf = userId == toInsertId;

        var delay = insertingSelf ? unit.EntryDelay : unit.DraggedEntryDelay;

        if (userId != null && !insertingSelf)
            _popupSystem.PopupEntity(Loc.GetString("disposal-unit-being-inserted", ("user", Identity.Entity((EntityUid)userId, EntityManager))), toInsertId, toInsertId, PopupType.Large);

        if (delay <= 0 || userId == null)
        {
            AfterInsert(unitId, unit, toInsertId, userId, doInsert: true);
            return true;
        }

        // Can't check if our target AND disposals moves currently so we'll just check target.
        // if you really want to check if disposals moves then add a predicate.
        var doAfterArgs = new DoAfterArgs(EntityManager, userId.Value, delay, new DisposalDoAfterEvent(), unitId, target: toInsertId, used: unitId)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = false,
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
        return true;
    }

    private void UpdateState(EntityUid uid, DisposalsPressureState state, DisposalUnitComponent component, MetaDataComponent metadata)
    {
        if (component.State == state)
            return;

        component.State = state;
        UpdateVisualState(uid, component);
        Dirty(uid, component, metadata);

        if (state == DisposalsPressureState.Ready)
        {
            component.NextPressurized = TimeSpan.Zero;

            // Manually engaged
            if (component.Engaged)
            {
                component.NextFlush = GameTiming.CurTime + component.ManualFlushTime;
            }
            else if (component.Container.ContainedEntities.Count > 0)
            {
                component.NextFlush = GameTiming.CurTime + component.AutomaticEngageTime;
            }
            else
            {
                component.NextFlush = null;
            }
        }
    }

    /// <summary>
    /// Work out if we can stop updating this disposals component i.e. full pressure and nothing colliding.
    /// </summary>
    private void Update(EntityUid uid, DisposalUnitComponent component, MetaDataComponent metadata)
    {
        var state = GetState(uid, component, metadata);

        // Pressurizing, just check if we need a state update.
        if (component.NextPressurized > GameTiming.CurTime)
        {
            UpdateState(uid, state, component, metadata);
            return;
        }

        if (component.NextFlush != null)
        {
            if (component.NextFlush.Value < GameTiming.CurTime)
            {
                TryFlush(uid, component);
            }
        }

        UpdateState(uid, state, component, metadata);
    }

    public bool TryFlush(EntityUid uid, DisposalUnitComponent component)
    {
        if (!CanFlush(uid, component))
        {
            return false;
        }

        if (component.NextFlush != null)
            component.NextFlush = component.NextFlush.Value + component.AutomaticEngageTime;

        var beforeFlushArgs = new BeforeDisposalFlushEvent();
        RaiseLocalEvent(uid, beforeFlushArgs);

        if (beforeFlushArgs.Cancelled)
        {
            Disengage(uid, component);
            return false;
        }

        var xform = Transform(uid);
        if (!TryComp(xform.GridUid, out MapGridComponent? grid))
            return false;

        var coords = xform.Coordinates;
        var entry = _map.GetLocal(xform.GridUid.Value, grid, coords)
            .FirstOrDefault(HasComp<Tube.DisposalEntryComponent>);

        if (entry == default || component is not DisposalUnitComponent sDisposals)
        {
            component.Engaged = false;
            UpdateUI((uid, component));
            Dirty(uid, component);
            return false;
        }

        HandleAir(uid, sDisposals, xform);

        _disposalTubeSystem.TryInsert(entry, sDisposals, beforeFlushArgs.Tags);

        component.NextPressurized = GameTiming.CurTime;
        if (!component.DisablePressure)
            component.NextPressurized += TimeSpan.FromSeconds(1f / PressurePerSecond);

        component.Engaged = false;
        // stop queuing NOW
        component.NextFlush = null;

        UpdateVisualState(uid, component, true);
        Dirty(uid, component);
        UpdateUI((uid, component));

        return true;
    }

    protected virtual void HandleAir(EntityUid uid, DisposalUnitComponent component, TransformComponent xform)
    {

    }

    public void ManualEngage(EntityUid uid, DisposalUnitComponent component, MetaDataComponent? metadata = null)
    {
        component.Engaged = true;
        UpdateVisualState(uid, component);
        Dirty(uid, component);
        UpdateUI((uid, component));

        if (!CanFlush(uid, component))
            return;

        if (!Resolve(uid, ref metadata))
            return;

        var pauseTime = Metadata.GetPauseTime(uid, metadata);
        var nextEngage = GameTiming.CurTime - pauseTime + component.ManualFlushTime;
        component.NextFlush = TimeSpan.FromSeconds(Math.Min((component.NextFlush ?? TimeSpan.MaxValue).TotalSeconds, nextEngage.TotalSeconds));
    }

    public void Disengage(EntityUid uid, DisposalUnitComponent component)
    {
        component.Engaged = false;

        if (component.Container.ContainedEntities.Count == 0)
        {
            component.NextFlush = null;
        }

        UpdateVisualState(uid, component);
        Dirty(uid, component);
        UpdateUI((uid, component));
    }

    /// <summary>
    /// Remove all entities currently in the disposal unit.
    /// </summary>
    public void TryEjectContents(EntityUid uid, DisposalUnitComponent component)
    {
        foreach (var entity in component.Container.ContainedEntities.ToArray())
        {
            Remove(uid, component, entity);
        }

        if (!component.Engaged)
        {
            component.NextFlush = null;
            Dirty(uid, component);
            UpdateUI((uid, component));
        }
    }

    /// <summary>
    /// If something is inserted (or the likes) then we'll queue up an automatic flush in the future.
    /// </summary>
    public void QueueAutomaticEngage(EntityUid uid, DisposalUnitComponent component, MetaDataComponent? metadata = null)
    {
        if (component.Deleted || !component.AutomaticEngage || !_power.IsPowered(uid) && component.Container.ContainedEntities.Count == 0)
        {
            return;
        }

        var pauseTime = Metadata.GetPauseTime(uid, metadata);
        var automaticTime = GameTiming.CurTime + component.AutomaticEngageTime - pauseTime;
        var flushTime = TimeSpan.FromSeconds(Math.Min((component.NextFlush ?? TimeSpan.MaxValue).TotalSeconds, automaticTime.TotalSeconds));

        component.NextFlush = flushTime;
        Dirty(uid, component);
        UpdateUI((uid, component));
    }

    private void OnUiButtonPressed(EntityUid uid, DisposalUnitComponent component, DisposalUnitComponent.UiButtonPressedMessage args)
    {
        if (args.Actor is not { Valid: true } player)
        {
            return;
        }

        switch (args.Button)
        {
            case DisposalUnitComponent.UiButton.Eject:
                TryEjectContents(uid, component);
                _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(player):player} hit eject button on {ToPrettyString(uid)}");
                break;
            case DisposalUnitComponent.UiButton.Engage:
                ToggleEngage(uid, component);
                _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(player):player} hit flush button on {ToPrettyString(uid)}, it's now {(component.Engaged ? "on" : "off")}");
                break;
            case DisposalUnitComponent.UiButton.Power:
                _power.TogglePower(uid, user: args.Actor);
                break;
            default:
                throw new ArgumentOutOfRangeException($"{ToPrettyString(player):player} attempted to hit a nonexistant button on {ToPrettyString(uid)}");
        }
    }

    public void ToggleEngage(EntityUid uid, DisposalUnitComponent component)
    {
        component.Engaged ^= true;

        if (component.Engaged)
        {
            ManualEngage(uid, component);
        }
        else
        {
            Disengage(uid, component);
        }
    }

    private void AddClimbInsideVerb(EntityUid uid, DisposalUnitComponent component, GetVerbsEvent<Verb> args)
    {
        // This is not an interaction, activation, or alternative verb type because unfortunately most users are
        // unwilling to accept that this is where they belong and don't want to accidentally climb inside.
        if (!args.CanAccess ||
            !args.CanInteract ||
            component.Container.ContainedEntities.Contains(args.User) ||
            !ActionBlockerSystem.CanMove(args.User))
        {
            return;
        }

        if (!CanInsert(uid, component, args.User))
            return;

        // Add verb to climb inside of the unit,
        Verb verb = new()
        {
            Act = () => TryInsert(uid, args.User, args.User),
            DoContactInteraction = true,
            Text = Loc.GetString("disposal-self-insert-verb-get-data-text")
        };
        // TODO VERB ICON
        // TODO VERB CATEGORY
        // create a verb category for "enter"?
        // See also, medical scanner. Also maybe add verbs for entering lockers/body bags?
        args.Verbs.Add(verb);
    }

    private void OnGetDumpableVerb(Entity<DisposalUnitComponent> ent, ref GetDumpableVerbEvent args)
    {
        args.Verb = Loc.GetString("dump-disposal-verb-name", ("unit", ent));
    }

    private void OnDump(Entity<DisposalUnitComponent> ent, ref DumpEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        args.PlaySound = true;

        foreach (var entity in args.DumpQueue)
        {
            DoInsertDisposalUnit(ent, entity, args.User);
        }
    }
}
