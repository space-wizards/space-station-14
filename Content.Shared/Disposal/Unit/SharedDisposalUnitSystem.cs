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
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly ClimbSystem _climb = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedDisposalHolderSystem _disposalHolder = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly INetManager _net = default!;

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

    protected void OnPreventCollide(Entity<DisposalUnitComponent> ent, ref PreventCollideEvent args)
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

        return _disposalHolder.TryEnterTube(holder, tube);
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
            !_container.Remove(toRemove, ent.Comp.Container))
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
