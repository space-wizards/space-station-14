using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Climbing.Systems;
using Content.Shared.Containers;
using Content.Shared.Database;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Holder;
using Content.Shared.Disposal.Tube;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Emag.Systems;
using Content.Shared.Explosion;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
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
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Shared.Disposal.Unit;

/// <summary>
/// This system handles all operations relating to disposal units.
/// </summary>
public abstract partial class SharedDisposalUnitSystem : EntitySystem
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
    [Dependency] private   readonly ISharedAdminLogManager _adminLogger = default!;
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

        SubscribeLocalEvent<DisposalUnitComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<DisposalUnitComponent, BeforeExplodeEvent>(OnExploded);
        SubscribeLocalEvent<DisposalUnitComponent, PowerChangedEvent>(OnPowerChange);
        SubscribeLocalEvent<DisposalUnitComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<DisposalUnitComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<DisposalUnitComponent, GotEmaggedEvent>(OnEmagged);

        // See SharedDisposalUnitSystem.Interactions
        SubscribeLocalEvent<DisposalUnitComponent, GetVerbsEvent<InteractionVerb>>(AddInteractionVerb);
        SubscribeLocalEvent<DisposalUnitComponent, GetVerbsEvent<AlternativeVerb>>(AddAltVerbs);
        SubscribeLocalEvent<DisposalUnitComponent, GetVerbsEvent<Verb>>(AddEnterOrExitVerb);
        SubscribeLocalEvent<DisposalUnitComponent, DisposalDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<DisposalUnitComponent, BeforeThrowInsertEvent>(OnThrowInsert);
        SubscribeLocalEvent<DisposalUnitComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<DisposalUnitComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<DisposalUnitComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<DisposalUnitComponent, ContainerRelayMovementEntityEvent>(OnMovement);
        SubscribeLocalEvent<DisposalUnitComponent, CanDropTargetEvent>(OnCanDragDropOn);
        SubscribeLocalEvent<DisposalUnitComponent, DragDropTargetEvent>(OnDragDropOn);
        SubscribeLocalEvent<DisposalUnitComponent, GetDumpableVerbEvent>(OnGetDumpableVerb);
        SubscribeLocalEvent<DisposalUnitComponent, DumpEvent>(OnDump);

        // See SharedDisposalUnitSystem.Visuals
        SubscribeLocalEvent<DisposalUnitComponent, DisposalUnitUiButtonPressedMessage>(OnUiButtonPressed);
    }

    #region: Event handling

    protected virtual void OnComponentInit(Entity<DisposalUnitComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Container = _container.EnsureContainer<Container>(ent, nameof(DisposalUnitComponent));
    }

    private void OnExploded(Entity<DisposalUnitComponent> ent, ref BeforeExplodeEvent args)
    {
        args.Contents.AddRange(GetContainedEntities(ent));
    }

    private void OnPowerChange(Entity<DisposalUnitComponent> ent, ref PowerChangedEvent args)
    {
        RecalculateFlushTime(ent, true);
        UpdateVisualState(ent);
    }

    private void OnAnchorChanged(Entity<DisposalUnitComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            return;

        EjectContents(ent);
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
                _adminLogger.AddStructured(LogType.Action, LogImpact.Medium, $"{args.User:player} inserted {args.Using.Value} into {uid}");
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

        _adminLogger.AddStructured(LogType.Action, LogImpact.Medium, $"{args.User:player} inserted {args.Used} into {uid}");
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

    protected void OnEmagged(Entity<DisposalUnitComponent> ent, ref GotEmaggedEvent args)
    {
        ent.Comp.DisablePressure = true;
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

        if (_whitelistSystem.IsWhitelistPass(component.Blacklist, entity) ||
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

        _adminLogger.AddStructured(LogType.Action, LogImpact.Medium, $"{user:player} inserted {toInsert} into {uid}");
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
            _adminLogger.AddStructured(LogType.Action, LogImpact.Medium, $"{user.Value:player} inserted {inserted} into {uid}");

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
                _adminLogger.AddStructured(LogType.Action, LogImpact.Low, $"{player:player} hit eject button on {uid}");
                break;
            case DisposalUnitComponent.UiButton.Engage:
                ToggleEngage(uid, component);
                _adminLogger.AddStructured(LogType.Action, LogImpact.Low, $"{player:player} hit flush button on {uid}, it's now {(component.Engaged ? "on" : "off")}");
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
            Insert(ent, entity, args.User);
        }
    }

    #endregion

    /// <summary>
    /// Checks whether a disposal unit can flush.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    /// <returns>True if the disposal unit can flush.</returns>
    public bool CanFlush(Entity<DisposalUnitComponent> ent)
    {
        return GetState(ent) == DisposalsPressureState.Ready
               && _power.IsPowered(ent.Owner)
               && Transform(ent).Anchored;
    }

    /// <summary>
    /// Gets the current pressure state of a disposals unit.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    /// <returns>The disposal unit's pressure state.</returns>
    public DisposalsPressureState GetState(Entity<DisposalUnitComponent> ent)
    {
        var nextPressure = ent.Comp.NextPressurized - _timing.CurTime;
        var pressurizeTime = 1f / ent.Comp.PressurePerSecond;
        var pressurizeDuration = pressurizeTime - ent.Comp.FlushDelay.TotalSeconds;

        if (nextPressure.TotalSeconds > pressurizeDuration)
            return DisposalsPressureState.Flushed;

        if (nextPressure > TimeSpan.Zero)
            return DisposalsPressureState.Pressurizing;

        return DisposalsPressureState.Ready;
    }

    /// <summary>
    /// Handles the actual insertion of an entity into a disposal unit.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    /// <param name="inserted">The entity inserted.</param>
    /// <param name="user">The one who inserted the entity.</param>
    /// <param name="doInsert">Do the insertion now.</param>
    protected void Insert(Entity<DisposalUnitComponent> ent, EntityUid inserted, EntityUid? user = null)
    {
        if (ent.Comp.Container != null)
        {
            // Note: the entity may have been inserted via SharedHandsSystem already
            _container.Insert(inserted, ent.Comp.Container);
        }

        if (!GetContainedEntities(ent).Contains(inserted))
            return;

        if (_timing.CurTime >= ent.Comp.NextAllowedInsertSound)
        {
            _audio.PlayPredicted(ent.Comp.InsertSound, ent, user: user);
            ent.Comp.NextAllowedInsertSound = _timing.CurTime + ent.Comp.InsertSoundDelay;
        }

        if (user != inserted && user != null)
        {
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user.Value):player} inserted {ToPrettyString(inserted)} into {ToPrettyString(ent)}");
        }

        AutomaticEngage(ent);

        _joints.RecursiveClearJoints(inserted);

        _ui.CloseUi(ent.Owner, DisposalUnitUiKey.Key, inserted);

        UpdateVisualState(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DisposalUnitComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var unit, out var metadata))
        {
            UpdateDisposalUnit((uid, unit), metadata);
        }
    }

    private void UpdateDisposalUnit(Entity<DisposalUnitComponent> ent, MetaDataComponent metadata)
    {
        var state = GetState(ent);

        // Check if we need a state update
        if (ent.Comp.NextPressurized > _timing.CurTime)
        {
            UpdateState(ent, state);
            return;
        }

        // Check if we need to flush
        if (ent.Comp.NextFlush != null &&
            ent.Comp.NextFlush.Value < _timing.CurTime)
        {
            TryFlush(ent);
        }

        UpdateState(ent, state);
    }

    private void UpdateState(Entity<DisposalUnitComponent> ent, DisposalsPressureState state)
    {
        if (ent.Comp.State == state)
            return;

        ent.Comp.State = state;

        if (state == DisposalsPressureState.Ready)
        {
            ent.Comp.NextPressurized = TimeSpan.Zero;
        }

        RecalculateFlushTime(ent, true);
        UpdateVisualState(ent);
    }

    /// <summary>
    /// Try to flush a disposal unit.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    /// <returns>True if the flush was successful.</returns>
    public bool TryFlush(Entity<DisposalUnitComponent> ent)
    {
        if (!CanFlush(ent))
            return false;

        var beforeFlushArgs = new BeforeDisposalFlushEvent();
        RaiseLocalEvent(ent, beforeFlushArgs);

        if (beforeFlushArgs.Cancelled)
        {
            SetEngage(ent, false);
            return false;
        }

        var xform = Transform(ent);

        if (!TryComp(xform.GridUid, out MapGridComponent? grid))
            return false;

        // Try to find an entry into disposals
        Entity<DisposalTubeComponent>? tube = null;

        foreach (var tubeUid in _map.GetLocal(xform.GridUid.Value, grid, xform.Coordinates))
        {
            if (HasComp<DisposalEntryComponent>(tubeUid) &&
                TryComp<DisposalTubeComponent>(tubeUid, out var tubeComp))
            {
                tube = new Entity<DisposalTubeComponent>(tubeUid, tubeComp);
                break;
            }
        }

        if (tube == null)
        {
            SetEngage(ent, false);
            return false;
        }

        // Try to transfer entities from the unit into disposals.
        TryTransfer(ent, tube.Value, beforeFlushArgs.Tags);

        ent.Comp.NextPressurized = _timing.CurTime;

        if (!ent.Comp.DisablePressure)
        {
            ent.Comp.NextPressurized += TimeSpan.FromSeconds(1f / ent.Comp.PressurePerSecond);
        }

        SetEngage(ent, false);
        return true;
    }

    /// <summary>
    /// Tries to transfer all entities in a disposal unit to the disposals system.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    /// <param name="tube">The tube leading into disposals.</param>
    /// <returns>True if the transfer was successful.</returns>
    public bool TryTransfer(Entity<DisposalUnitComponent> ent, Entity<DisposalTubeComponent> tube, IEnumerable<string>? tags = null)
    {
        if (_net.IsClient && !_timing.IsFirstTimePredicted)
            return false;

        if (GetContainedEntityCount(ent) == 0)
            return false;

        var xform = Transform(ent);
        var holderUid = Spawn(ent.Comp.HolderPrototypeId, _xform.GetMapCoordinates(ent, xform: xform));
        var holder = new Entity<DisposalHolderComponent>(holderUid, Comp<DisposalHolderComponent>(holderUid));

        if (holder.Comp.Container == null)
            return false;

        foreach (var entity in GetContainedEntities(ent))
        {
            _container.Insert(entity, holder.Comp.Container);
            _disposalHolder.AttachEntity((holderUid, holder), entity);
        }

        if (tags != null)
        {
            foreach (var tag in tags)
            {
                _disposalHolder.AddTag(holder, tag);
            }

            Dirty(holder);
        }

        // Take in the local atmos and pass it to the spawned disposals holder.
        IntakeAir(ent, xform);
        _disposalHolder.TransferAtmos(holder, ent);

        return _disposalHolder.TryEnterTube(holder, (tube, tube.Comp));
    }

    /// <summary>
    /// Remove all entities currently in a disposal unit.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    public void EjectContents(Entity<DisposalUnitComponent> ent)
    {
        foreach (var toRemove in GetContainedEntities(ent))
        {
            Remove(ent, toRemove);
        }

        UpdateVisualState(ent);
    }

    /// <summary>
    /// Remove an entity from a disposal unit.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    /// <param name="toRemove">The entity to remove.</param>
    public void Remove(Entity<DisposalUnitComponent> ent, EntityUid toRemove)
    {
        if (_timing.ApplyingState)
            return;

        if (!Terminating(toRemove) &&
            ent.Comp.Container != null &&
            _container.Remove(toRemove, ent.Comp.Container))
        {
            _climb.Climb(toRemove, toRemove, ent, silent: true);
        }

        RecalculateFlushTime(ent);
        UpdateVisualState(ent);
    }

    /// <summary>
    /// Recalculates when a disposal will flush next
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    /// <param name="reset">Reset any in-progress flushes.</param>
    public void RecalculateFlushTime(Entity<DisposalUnitComponent> ent, bool reset = false)
    {
        TimeSpan? nextFlush = null;

        // Reset next flush?
        if (reset)
        {
            ent.Comp.NextFlush = null;
        }

        // If engaged/automatically engaged, figure out the next flush time
        if (ent.Comp.Engaged || GetContainedEntityCount(ent) > 0)
        {
            var newFlush = ent.Comp.Engaged
                ? _timing.CurTime + ent.Comp.ManualFlushTime
                : _timing.CurTime + ent.Comp.AutomaticEngageTime;

            if (GetState(ent) != DisposalsPressureState.Ready)
            {
                newFlush += ent.Comp.NextPressurized;
            }

            nextFlush = (ent.Comp.NextFlush ?? TimeSpan.MaxValue);

            // Check if there is a flush in progress that will occur
            // sooner than what was calculated
            nextFlush = TimeSpan.FromSeconds(Math.Min(nextFlush.Value.TotalSeconds, newFlush.TotalSeconds));
        }

        ent.Comp.NextFlush = nextFlush;
        Dirty(ent);

        UpdateUI(ent);
    }

    /// <summary>
    /// Sets a disposal unit to move towards flushing itself.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    /// <param name="metadata">The disposal unit's metadata.</param>
    public void SetEngage(Entity<DisposalUnitComponent> ent, bool engaged)
    {
        ent.Comp.Engaged = engaged;

        RecalculateFlushTime(ent);
        UpdateVisualState(ent);
    }

    /// <summary>
    /// Primes a disposal unit to automatically flush sometime in the future.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    /// <param name="metadata">The disposal unit's metadata.</param>
    public void AutomaticEngage(Entity<DisposalUnitComponent> ent)
    {
        if (!ent.Comp.AutomaticEngage)
            return;

        RecalculateFlushTime(ent);
    }

    /// <summary>
    /// Toggles a disposal unit between 'engaged' and 'disengaged'.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    public void ToggleEngage(Entity<DisposalUnitComponent> ent)
    {
        SetEngage(ent, !ent.Comp.Engaged);
    }

    /// <summary>
    /// All entities contained in a disposal unit.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    /// <returns>A copy of the disposal unit's ContainedEntities list.</returns>
    public IReadOnlyList<EntityUid> GetContainedEntities(Entity<DisposalUnitComponent> ent)
    {
        if (ent.Comp.Container == null)
            return new List<EntityUid>();

        return ent.Comp.Container.ContainedEntities.ToList();
    }

    /// <summary>
    /// The number of entities contained in a disposal unit.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    /// <returns>The entity count.</returns>
    public int GetContainedEntityCount(Entity<DisposalUnitComponent> ent)
    {
        return GetContainedEntities(ent).Count;
    }

    /// <summary>
    /// Takes the atmos surrounding the disposal unit into itself.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    /// <param name="xform">The disposal unit's transform.</param>
    protected virtual void IntakeAir(Entity<DisposalUnitComponent> ent, TransformComponent xform)
    {
        // Handled by the server
    }
}
