using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Climbing.Systems;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Containers;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Tube;
using Content.Shared.Disposal.Unit.Events;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Emag.Systems;
using Content.Shared.Explosion;
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
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.Disposal.Unit;

public abstract class SharedDisposalUnitSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly ClimbSystem _climb = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedDisposalTubeSystem _disposalTubeSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    private static TimeSpan _exitAttemptDelay = TimeSpan.FromSeconds(0.5);

    // Percentage
    public const float PressurePerSecond = 0.05f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalUnitComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<DisposalUnitComponent, BeforeExplodeEvent>(OnExploded);

        SubscribeLocalEvent<DisposalUnitComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<DisposalUnitComponent, CanDropTargetEvent>(OnCanDragDropOn);
        SubscribeLocalEvent<DisposalUnitComponent, GetVerbsEvent<InteractionVerb>>(AddInsertVerb);
        SubscribeLocalEvent<DisposalUnitComponent, GetVerbsEvent<AlternativeVerb>>(AddDisposalAltVerbs);
        SubscribeLocalEvent<DisposalUnitComponent, GetVerbsEvent<Verb>>(AddClimbInsideVerb);

        SubscribeLocalEvent<DisposalUnitComponent, DisposalDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<DisposalUnitComponent, BeforeThrowInsertEvent>(OnThrowInsert);

        SubscribeLocalEvent<DisposalUnitComponent, DisposalUnitUiButtonPressedMessage>(OnUiButtonPressed);

        SubscribeLocalEvent<DisposalUnitComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<DisposalUnitComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<DisposalUnitComponent, PowerChangedEvent>(OnPowerChange);
        SubscribeLocalEvent<DisposalUnitComponent, ComponentInit>(OnComponentInit);

        SubscribeLocalEvent<DisposalUnitComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<DisposalUnitComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<DisposalUnitComponent, DragDropTargetEvent>(OnDragDropOn);
        SubscribeLocalEvent<DisposalUnitComponent, ContainerRelayMovementEntityEvent>(OnMovement);

        SubscribeLocalEvent<DisposalUnitComponent, GetDumpableVerbEvent>(OnGetDumpableVerb);
        SubscribeLocalEvent<DisposalUnitComponent, DumpEvent>(OnDump);
    }

    protected virtual void HandleAir(Entity<DisposalUnitComponent> ent, TransformComponent xform)
    {

    }

    private void OnDestruction(Entity<DisposalUnitComponent> ent, ref DestructionEventArgs args)
    {
        TryEjectContents(ent);
    }

    private void OnExploded(Entity<DisposalUnitComponent> ent, ref BeforeExplodeEvent args)
    {
        args.Contents.AddRange(ent.Comp.Container.ContainedEntities);
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
                Act = () => ManualEngage(ent),
                Text = Loc.GetString("disposal-flush-verb-get-data-text"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/delete_transparent.svg.192dpi.png")),
                Priority = 1,
            };
            args.Verbs.Add(flushVerb);

            // Verb to eject the contents
            AlternativeVerb ejectVerb = new()
            {
                Act = () => TryEjectContents(ent),
                Category = VerbCategory.Eject,
                Text = Loc.GetString("disposal-eject-verb-get-data-text")
            };
            args.Verbs.Add(ejectVerb);
        }
    }

    private void AddInsertVerb(Entity<DisposalUnitComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || args.Using == null)
            return;

        if (!_actionBlockerSystem.CanDrop(args.User))
            return;

        if (!CanInsert(ent, args.Using.Value))
            return;

        var verbData = args;

        InteractionVerb insertVerb = new()
        {
            Text = Name(args.Using.Value),
            Category = VerbCategory.Insert,
            Act = () =>
            {
                _handsSystem.TryDropIntoContainer((verbData.User, verbData.Hands), verbData.Using.Value, ent.Comp.Container, checkActionBlocker: false);
                _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(verbData.User):player} inserted {ToPrettyString(verbData.Using.Value)} into {ToPrettyString(ent)}");
                AfterInsert(ent, verbData.Using.Value, verbData.User);
            }
        };

        args.Verbs.Add(insertVerb);
    }

    private void OnDoAfter(Entity<DisposalUnitComponent> ent, ref DisposalDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null || args.Args.Used == null)
            return;

        AfterInsert(ent, args.Args.Target.Value, args.Args.User, doInsert: true);

        args.Handled = true;
    }

    private void OnThrowInsert(Entity<DisposalUnitComponent> ent, ref BeforeThrowInsertEvent args)
    {
        if (!CanInsert(ent, args.ThrownEntity))
            args.Cancelled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DisposalUnitComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var unit, out var metadata))
        {
            Update((uid, unit), metadata);
        }
    }

    // TODO: This should just use the same thing as entity storage?
    private void OnMovement(Entity<DisposalUnitComponent> ent, ref ContainerRelayMovementEntityEvent args)
    {
        var currentTime = _timing.CurTime;

        if (!_actionBlockerSystem.CanMove(args.Entity))
            return;

        if (!TryComp(args.Entity, out HandsComponent? hands) ||
            hands.Count == 0 ||
            currentTime < ent.Comp.LastExitAttempt + _exitAttemptDelay)
            return;

        Dirty(ent);
        ent.Comp.LastExitAttempt = currentTime;
        Remove(ent, args.Entity);
        UpdateUI(ent);
    }

    private void OnActivate(Entity<DisposalUnitComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        args.Handled = true;
        _ui.TryToggleUi(ent.Owner, DisposalUnitUiKey.Key, args.User);
    }

    private void OnAfterInteractUsing(Entity<DisposalUnitComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (!HasComp<HandsComponent>(args.User))
        {
            return;
        }

        if (!CanInsert(ent, args.Used) || !_handsSystem.TryDropIntoContainer(args.User, args.Used, ent.Comp.Container))
        {
            return;
        }

        _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User):player} inserted {ToPrettyString(args.Used)} into {ToPrettyString(ent)}");
        AfterInsert(ent, args.Used, args.User);
        args.Handled = true;
    }

    protected virtual void OnComponentInit(Entity<DisposalUnitComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Container = _containers.EnsureContainer<Container>(ent, DisposalUnitComponent.ContainerId);
    }

    private void OnPowerChange(Entity<DisposalUnitComponent> ent, ref PowerChangedEvent args)
    {
        if (!ent.Comp.Running)
            return;

        UpdateUI(ent);
        UpdateVisualState(ent);

        if (!args.Powered)
        {
            ent.Comp.NextFlush = null;
            Dirty(ent);
            return;
        }

        if (ent.Comp.Engaged)
        {
            // Run ManualEngage to recalculate a new flush time
            ManualEngage(ent);
        }
    }

    private void OnAnchorChanged(Entity<DisposalUnitComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (Terminating(ent))
            return;

        UpdateVisualState(ent);

        if (!args.Anchored)
            TryEjectContents(ent);
    }

    private void OnDragDropOn(Entity<DisposalUnitComponent> ent, ref DragDropTargetEvent args)
    {
        args.Handled = TryInsert(ent, args.Dragged, args.User);
    }

    private void UpdateUI(Entity<DisposalUnitComponent> entity)
    {

    }

    /// <summary>
    /// Returns the estimated time when the disposal unit will be back to full pressure.
    /// </summary>
    public TimeSpan EstimatedFullPressure(Entity<DisposalUnitComponent> ent)
    {
        if (ent.Comp.NextPressurized < _timing.CurTime)
            return TimeSpan.Zero;

        return ent.Comp.NextPressurized;
    }

    public bool CanFlush(Entity<DisposalUnitComponent> ent)
    {
        return GetState(ent) == DisposalsPressureState.Ready
               && _power.IsPowered(ent.Owner)
               && Comp<TransformComponent>(ent).Anchored;
    }

    public void Remove(Entity<DisposalUnitComponent> ent, EntityUid toRemove)
    {
        if (_timing.ApplyingState)
            return;

        if (!_containers.Remove(toRemove, ent.Comp.Container))
            return;

        if (ent.Comp.Container.ContainedEntities.Count == 0)
        {
            // If not manually engaged then reset the flushing entirely.
            if (!ent.Comp.Engaged)
            {
                ent.Comp.NextFlush = null;
                Dirty(ent);
                UpdateUI(ent);
            }
        }

        _climb.Climb(toRemove, toRemove, ent, silent: true);

        UpdateVisualState(ent);
    }

    public void UpdateVisualState(Entity<DisposalUnitComponent> ent, bool flush = false)
    {
        if (!TryComp(ent, out AppearanceComponent? appearance))
            return;

        var isAnchored = Transform(ent).Anchored;
        _appearance.SetData(ent, AnchorVisuals.Anchored, isAnchored, appearance);

        if (!isAnchored)
            return;

        var state = GetState(ent);
        _appearance.SetData(ent, DisposalUnitVisuals.IsReady, state == DisposalsPressureState.Ready, appearance);
        _appearance.SetData(ent, DisposalUnitVisuals.IsFlushing, state == DisposalsPressureState.Flushed, appearance);
        _appearance.SetData(ent, DisposalUnitVisuals.IsEngaged, ent.Comp.Engaged, appearance);

        if (!_power.IsPowered(ent.Owner))
            return;

        _appearance.SetData(ent, DisposalUnitVisuals.IsFull, ent.Comp.Container.ContainedEntities.Count > 0, appearance);
        _appearance.SetData(ent, DisposalUnitVisuals.IsCharging, state != DisposalsPressureState.Ready, appearance);
    }

    /// <summary>
    /// Gets the current pressure state of a disposals unit.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="metadata"></param>
    /// <returns></returns>
    public DisposalsPressureState GetState(Entity<DisposalUnitComponent> ent, MetaDataComponent? metadata = null)
    {
        var nextPressure = _metaData.GetPauseTime(ent, metadata) + ent.Comp.NextPressurized - _timing.CurTime;
        var pressurizeTime = 1f / PressurePerSecond;
        var pressurizeDuration = pressurizeTime - ent.Comp.FlushDelay.TotalSeconds;

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

    public float GetPressure(Entity<DisposalUnitComponent> ent, MetaDataComponent? metadata = null)
    {
        if (!Resolve(ent, ref metadata))
            return 0f;

        var pauseTime = _metaData.GetPauseTime(ent, metadata);
        return MathF.Min(1f,
            (float)(_timing.CurTime - pauseTime - ent.Comp.NextPressurized).TotalSeconds / PressurePerSecond);
    }

    protected void OnPreventCollide(Entity<DisposalUnitComponent> ent, ref PreventCollideEvent args)
    {
        var otherBody = args.OtherEntity;

        // Items dropped shouldn't collide but items thrown should
        if (HasComp<ItemComponent>(otherBody) && !HasComp<ThrownItemComponent>(otherBody))
        {
            args.Cancelled = true;
        }
    }

    protected void OnCanDragDropOn(Entity<DisposalUnitComponent> ent, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.CanDrop = CanInsert(ent, args.Dragged);
        args.Handled = true;
    }

    protected void OnEmagged(Entity<DisposalUnitComponent> ent, ref GotEmaggedEvent args)
    {
        ent.Comp.DisablePressure = true;
        args.Handled = true;
    }

    public bool CanInsert(Entity<DisposalUnitComponent> ent, EntityUid toInsert)
    {
        // TODO: All of the below should be using the EXISTING EVENT
        if (!_containers.CanInsert(toInsert, ent.Comp.Container))
            return false;

        if (!Transform(ent).Anchored)
            return false;

        var storable = HasComp<ItemComponent>(toInsert);
        if (!storable && !HasComp<MobStateComponent>(toInsert))
            return false;

        if (_whitelistSystem.IsBlacklistPass(ent.Comp.Blacklist, toInsert) ||
            _whitelistSystem.IsWhitelistFail(ent.Comp.Whitelist, toInsert))
            return false;

        if (TryComp<PhysicsComponent>(toInsert, out var physics) && (physics.CanCollide) || storable)
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

        if (!_containers.Insert(toInsert, disposal.Container))
            return;

        _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user):player} inserted {ToPrettyString(toInsert)} into {ToPrettyString(uid)}");
        AfterInsert((uid, disposal), toInsert, user);
    }

    public void AfterInsert(Entity<DisposalUnitComponent> ent,
        EntityUid inserted,
        EntityUid? user = null,
        bool doInsert = false)
    {
        _audio.PlayPredicted(ent.Comp.InsertSound, ent, user: user);
        if (doInsert && !_containers.Insert(inserted, ent.Comp.Container))
            return;

        if (user != inserted && user != null)
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user.Value):player} inserted {ToPrettyString(inserted)} into {ToPrettyString(ent)}");

        QueueAutomaticEngage(ent);

        _ui.CloseUi(ent.Owner, DisposalUnitUiKey.Key, inserted);

        // Maybe do pullable instead? Eh still fine.
        _joints.RecursiveClearJoints(inserted);
        UpdateVisualState(ent);
    }

    public bool TryInsert(Entity<DisposalUnitComponent> ent, EntityUid toInsertId, EntityUid? userId)
    {
        if (userId.HasValue && !HasComp<HandsComponent>(userId) && toInsertId != userId) // Mobs like mouse can Jump inside even with no hands
        {
            _popupSystem.PopupEntity(Loc.GetString("disposal-unit-no-hands"), userId.Value, userId.Value, PopupType.SmallCaution);
            return false;
        }

        if (!CanInsert(ent, toInsertId))
            return false;

        bool insertingSelf = userId == toInsertId;

        var delay = insertingSelf ? ent.Comp.EntryDelay : ent.Comp.DraggedEntryDelay;

        if (userId != null && !insertingSelf)
            _popupSystem.PopupEntity(Loc.GetString("disposal-unit-being-inserted", ("user", Identity.Entity((EntityUid)userId, EntityManager))), toInsertId, toInsertId, PopupType.Large);

        if (delay <= 0 || userId == null)
        {
            AfterInsert(ent, toInsertId, userId, doInsert: true);
            return true;
        }

        // Can't check if our target AND disposals moves currently so we'll just check target.
        // if you really want to check if disposals moves then add a predicate.
        var doAfterArgs = new DoAfterArgs(EntityManager, userId.Value, delay, new DisposalDoAfterEvent(), ent, target: toInsertId, used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = false,
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
        return true;
    }

    private void UpdateState(Entity<DisposalUnitComponent> ent, DisposalsPressureState state)
    {
        if (ent.Comp.State == state)
            return;

        ent.Comp.State = state;
        UpdateVisualState(ent);
        Dirty(ent);

        if (state == DisposalsPressureState.Ready)
        {
            ent.Comp.NextPressurized = TimeSpan.Zero;

            // Manually engaged
            if (ent.Comp.Engaged)
            {
                ent.Comp.NextFlush = _timing.CurTime + ent.Comp.ManualFlushTime;
            }
            else if (ent.Comp.Container.ContainedEntities.Count > 0)
            {
                ent.Comp.NextFlush = _timing.CurTime + ent.Comp.AutomaticEngageTime;
            }
            else
            {
                ent.Comp.NextFlush = null;
            }
        }
    }

    /// <summary>
    /// Work out if we can stop updating this disposals component i.e. full pressure and nothing colliding.
    /// </summary>
    private void Update(Entity<DisposalUnitComponent> ent, MetaDataComponent metadata)
    {
        var state = GetState(ent, metadata);

        // Pressurizing, just check if we need a state update.
        if (ent.Comp.NextPressurized > _timing.CurTime)
        {
            UpdateState(ent, state);
            return;
        }

        if (ent.Comp.NextFlush != null)
        {
            if (ent.Comp.NextFlush.Value < _timing.CurTime)
            {
                TryFlush(ent);
            }
        }

        UpdateState(ent, state);
    }

    public bool TryFlush(Entity<DisposalUnitComponent> ent)
    {
        if (!CanFlush(ent))
        {
            return false;
        }

        if (ent.Comp.NextFlush != null)
            ent.Comp.NextFlush = ent.Comp.NextFlush.Value + ent.Comp.AutomaticEngageTime;

        var beforeFlushArgs = new BeforeDisposalFlushEvent();
        RaiseLocalEvent(ent, beforeFlushArgs);

        if (beforeFlushArgs.Cancelled)
        {
            Disengage(ent);
            return false;
        }

        var xform = Transform(ent);
        if (!TryComp(xform.GridUid, out MapGridComponent? grid))
            return false;

        DisposalEntryComponent? entryComp = null;
        DisposalTubeComponent? tubeComp = null;

        var coords = xform.Coordinates;
        var entry = _map.GetLocal(xform.GridUid.Value, grid, coords)
            .FirstOrNull(x => TryComp(x, out entryComp) && TryComp(x, out tubeComp));

        if (entry == null || entryComp == null || tubeComp == null)
        {
            ent.Comp.Engaged = false;
            UpdateUI(ent);
            Dirty(ent);
            return false;
        }

        HandleAir(ent, xform);

        _disposalTubeSystem.TryInsert((entry.Value, entryComp, tubeComp), ent, beforeFlushArgs.Tags);

        ent.Comp.NextPressurized = _timing.CurTime;
        if (!ent.Comp.DisablePressure)
            ent.Comp.NextPressurized += TimeSpan.FromSeconds(1f / PressurePerSecond);

        ent.Comp.Engaged = false;
        // stop queuing NOW
        ent.Comp.NextFlush = null;

        UpdateVisualState(ent, true);
        Dirty(ent);
        UpdateUI(ent);

        return true;
    }

    public void ManualEngage(Entity<DisposalUnitComponent> ent, MetaDataComponent? metadata = null)
    {
        ent.Comp.Engaged = true;
        UpdateVisualState(ent);
        Dirty(ent);
        UpdateUI(ent);

        if (!CanFlush(ent))
            return;

        if (!Resolve(ent, ref metadata))
            return;

        var pauseTime = _metaData.GetPauseTime(ent, metadata);
        var nextEngage = _timing.CurTime - pauseTime + ent.Comp.ManualFlushTime;
        ent.Comp.NextFlush = TimeSpan.FromSeconds(Math.Min((ent.Comp.NextFlush ?? TimeSpan.MaxValue).TotalSeconds, nextEngage.TotalSeconds));
    }

    public void Disengage(Entity<DisposalUnitComponent> ent)
    {
        ent.Comp.Engaged = false;

        if (ent.Comp.Container.ContainedEntities.Count == 0)
        {
            ent.Comp.NextFlush = null;
        }

        UpdateVisualState(ent);
        Dirty(ent);
        UpdateUI(ent);
    }

    /// <summary>
    /// Remove all entities currently in the disposal unit.
    /// </summary>
    public void TryEjectContents(Entity<DisposalUnitComponent> ent)
    {
        foreach (var toRemove in ent.Comp.Container.ContainedEntities.ToArray())
        {
            Remove(ent, toRemove);
        }

        if (!ent.Comp.Engaged)
        {
            ent.Comp.NextFlush = null;
            Dirty(ent);
            UpdateUI(ent);
        }
    }

    /// <summary>
    /// If something is inserted (or the likes) then we'll queue up an automatic flush in the future.
    /// </summary>
    public void QueueAutomaticEngage(Entity<DisposalUnitComponent> ent, MetaDataComponent? metadata = null)
    {
        if (ent.Comp.Deleted || !ent.Comp.AutomaticEngage || !_power.IsPowered(ent.Owner) && ent.Comp.Container.ContainedEntities.Count == 0)
        {
            return;
        }

        var pauseTime = _metaData.GetPauseTime(ent, metadata);
        var automaticTime = _timing.CurTime + ent.Comp.AutomaticEngageTime - pauseTime;
        var flushTime = TimeSpan.FromSeconds(Math.Min((ent.Comp.NextFlush ?? TimeSpan.MaxValue).TotalSeconds, automaticTime.TotalSeconds));

        ent.Comp.NextFlush = flushTime;
        Dirty(ent);
        UpdateUI(ent);
    }

    private void OnUiButtonPressed(Entity<DisposalUnitComponent> ent, ref DisposalUnitUiButtonPressedMessage args)
    {
        if (args.Actor is not { Valid: true } player)
        {
            return;
        }

        switch (args.Button)
        {
            case DisposalUnitUiButton.Eject:
                TryEjectContents(ent);
                _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(player):player} hit eject button on {ToPrettyString(ent)}");
                break;
            case DisposalUnitUiButton.Engage:
                ToggleEngage(ent);
                _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(player):player} hit flush button on {ToPrettyString(ent)}, it's now {(ent.Comp.Engaged ? "on" : "off")}");
                break;
            case DisposalUnitUiButton.Power:
                _power.TogglePower(ent.Owner, user: args.Actor);
                break;
            default:
                throw new ArgumentOutOfRangeException($"{ToPrettyString(player):player} attempted to hit a nonexistant button on {ToPrettyString(ent)}");
        }
    }

    public void ToggleEngage(Entity<DisposalUnitComponent> ent)
    {
        ent.Comp.Engaged ^= true;

        if (ent.Comp.Engaged)
        {
            ManualEngage(ent);
        }
        else
        {
            Disengage(ent);
        }
    }

    private void AddClimbInsideVerb(Entity<DisposalUnitComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        // This is not an interaction, activation, or alternative verb type because unfortunately most users are
        // unwilling to accept that this is where they belong and don't want to accidentally climb inside.
        if (!args.CanAccess ||
            !args.CanInteract ||
            ent.Comp.Container.ContainedEntities.Contains(args.User) ||
            !_actionBlockerSystem.CanMove(args.User))
        {
            return;
        }

        if (!CanInsert(ent, args.User))
            return;

        var verbData = args;

        // Add verb to climb inside of the unit,
        Verb verb = new()
        {
            Act = () => TryInsert(ent, verbData.User, verbData.User),
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
