using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Climbing.Systems;
using Content.Shared.Containers;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Holder;
using Content.Shared.Disposal.Tube;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Emag.Systems;
using Content.Shared.Explosion;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
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
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.Disposal.Unit;

/// <summary>
/// This system handles all operations relating to disposal units.
/// </summary>
public abstract partial class SharedDisposalUnitSystem : EntitySystem
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
    [Dependency] private readonly DisposalTubeSystem _disposalTube = default!;
    [Dependency] private readonly SharedDisposalHolderSystem _disposalHolder = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalUnitComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<DisposalUnitComponent, DestructionEventArgs>(OnDestruction);
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
        ent.Comp.Container = _containers.EnsureContainer<Container>(ent, nameof(DisposalUnitComponent));
    }

    private void OnDestruction(Entity<DisposalUnitComponent> ent, ref DestructionEventArgs args)
    {
        EjectContents(ent);
    }

    private void OnExploded(Entity<DisposalUnitComponent> ent, ref BeforeExplodeEvent args)
    {
        args.Contents.AddRange(GetContainedEntities(ent));
    }

    private void OnPowerChange(Entity<DisposalUnitComponent> ent, ref PowerChangedEvent args)
    {
        if (!ent.Comp.Running)
            return;

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

        if (!args.Anchored)
        {
            EjectContents(ent);
        }
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
            DoInsertDisposalUnit(ent, entity, args.User);
        }
    }

    #endregion

    /// <summary>
    /// Returns the estimated time when the disposal unit will be back to full pressure.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    /// <returns>The estimated time.</returns>
    public TimeSpan EstimatedFullPressure(Entity<DisposalUnitComponent> ent)
    {
        if (ent.Comp.NextPressurized < _timing.CurTime)
            return TimeSpan.Zero;

        return ent.Comp.NextPressurized;
    }

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
    /// Remove an entity from a disposal unit.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    /// <param name="toRemove">The entity to remove.</param>
    public void Remove(Entity<DisposalUnitComponent> ent, EntityUid toRemove)
    {
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.Container == null || !_containers.Remove(toRemove, ent.Comp.Container))
            return;

        if (ent.Comp.Container.ContainedEntities.Count == 0 &&
            !ent.Comp.Engaged)
        {
            ent.Comp.NextFlush = null;
            Dirty(ent);
        }

        _climb.Climb(toRemove, toRemove, ent, silent: true);

        UpdateVisualState(ent);
    }

    /// <summary>
    /// Gets the current pressure state of a disposals unit.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    /// <param name="metadata"></param>
    /// <returns>The disposal unit's pressure state.</returns>
    public DisposalsPressureState GetState(Entity<DisposalUnitComponent> ent, MetaDataComponent? metadata = null)
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
    public void Insert(Entity<DisposalUnitComponent> ent,
        EntityUid inserted,
        EntityUid? user = null,
        bool doInsert = false)
    {
        if (doInsert && (ent.Comp.Container == null || !_containers.Insert(inserted, ent.Comp.Container)))
            return;

        if (_timing.CurTime >= ent.Comp.NextAllowedInsertSound)
        {
            _audio.PlayPredicted(ent.Comp.InsertSound, ent, user: user);
            ent.Comp.NextAllowedInsertSound = _timing.CurTime + ent.Comp.InsertSoundDelay;
        }

        if (user != inserted && user != null)
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user.Value):player} inserted {ToPrettyString(inserted)} into {ToPrettyString(ent)}");

        QueueAutomaticEngage(ent);

        _ui.CloseUi(ent.Owner, DisposalUnitUiKey.Key, inserted);

        // Maybe do pullable instead? Eh still fine.
        _joints.RecursiveClearJoints(inserted);
        UpdateVisualState(ent);
    }

    /// <summary>
    /// Tries to insert an entity into a disposal unit.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    /// <param name="toInsert">The entity to insert.</param>
    /// <param name="user">The one inserting the entity.</param>
    /// <returns>True if the entity can be inserted.</returns>
    public bool TryInsert(Entity<DisposalUnitComponent> ent, EntityUid toInsert, EntityUid? user)
    {
        if (user.HasValue && !HasComp<HandsComponent>(user) && toInsert != user) // Mobs like mouse can Jump inside even with no hands
        {
            _popupSystem.PopupEntity(Loc.GetString("disposal-unit-no-hands"), user.Value, user.Value, PopupType.SmallCaution);
            return false;
        }

        if (ent.Comp.Container == null || !_containers.CanInsert(toInsert, ent.Comp.Container))
            return false;

        bool insertingSelf = user == toInsert;

        var delay = insertingSelf ? ent.Comp.EntryDelay : ent.Comp.DraggedEntryDelay;

        if (user != null && !insertingSelf)
        {
            _popupSystem.PopupEntity(Loc.GetString("disposal-unit-being-inserted",
                ("user", Identity.Entity((EntityUid)user, EntityManager))),
                toInsert,
                toInsert,
                PopupType.Large);
        }

        if (delay <= 0 || user == null)
        {
            Insert(ent, toInsert, user, doInsert: true);
            return true;
        }

        // Can't check if our target AND disposals moves currently so we'll just check target.
        // if you really want to check if disposals moves then add a predicate.
        var doAfterArgs = new DoAfterArgs(EntityManager, user.Value, delay, new DisposalDoAfterEvent(), ent, target: toInsert, used: ent)
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
        UpdateUI(ent);
        Dirty(ent);

        if (state == DisposalsPressureState.Ready)
        {
            ent.Comp.NextPressurized = TimeSpan.Zero;

            // Manually engaged
            if (ent.Comp.Engaged)
            {
                ent.Comp.NextFlush = _timing.CurTime + ent.Comp.ManualFlushTime;
            }
            else if (GetContainedEntityCount(ent) > 0)
            {
                ent.Comp.NextFlush = _timing.CurTime + ent.Comp.AutomaticEngageTime;
            }
            else
            {
                ent.Comp.NextFlush = null;
            }
        }
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

    /// <summary>
    /// Work out if we can stop updating this disposals component
    /// (i.e. full pressure and nothing colliding).
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

        // Check if we need to flush
        if (ent.Comp.NextFlush != null &&
            ent.Comp.NextFlush.Value < _timing.CurTime)
        {
            TryFlush(ent);
        }

        UpdateState(ent, state);
    }

    /// <summary>
    /// Try to flush the disposal unit.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    /// <returns>True if the flush was successful.</returns>
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

        DisposalTubeComponent? tubeComp = null;

        var coords = xform.Coordinates;
        var tubeUid = _map.GetLocal(xform.GridUid.Value, grid, coords)
            .FirstOrNull(x => HasComp<DisposalEntryComponent>(x) && TryComp(x, out tubeComp));

        if (tubeUid == null || tubeComp == null)
        {
            ent.Comp.Engaged = false;

            UpdateVisualState(ent);
            UpdateUI(ent);
            Dirty(ent);

            return false;
        }

        // Try to transfer entities from the unit into disposals.
        // If successful, take in the local atmos and pass it
        // to the spawned disposals holder.
        if (ent.Comp.Container != null &&
            _disposalTube.TryInsert
                ((tubeUid.Value, tubeComp),
                ent.Comp.Container.ContainedEntities.ToArray(),
                ent.Comp.HolderPrototypeId,
                out var holderEnt,
                beforeFlushArgs.Tags))
        {
            IntakeAir(ent, xform);
            _disposalHolder.TransferAtmos(holderEnt.Value, ent);
        }

        ent.Comp.NextPressurized = _timing.CurTime;
        if (!ent.Comp.DisablePressure)
            ent.Comp.NextPressurized += TimeSpan.FromSeconds(1f / ent.Comp.PressurePerSecond);

        ent.Comp.Engaged = false;
        ent.Comp.NextFlush = null;

        UpdateVisualState(ent);
        UpdateUI(ent);
        Dirty(ent);

        return true;
    }

    /// <summary>
    /// Sets a disposal unit to move towards flushing itself.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    /// <param name="metadata">The disposal unit's metadata.</param>
    public void ManualEngage(Entity<DisposalUnitComponent> ent, MetaDataComponent? metadata = null)
    {
        ent.Comp.Engaged = true;

        UpdateVisualState(ent);
        UpdateUI(ent);
        Dirty(ent);

        if (!CanFlush(ent))
            return;

        if (!Resolve(ent, ref metadata))
            return;

        var pauseTime = _metaData.GetPauseTime(ent, metadata);
        var nextEngage = _timing.CurTime - pauseTime + ent.Comp.ManualFlushTime;
        ent.Comp.NextFlush = TimeSpan.FromSeconds(Math.Min((ent.Comp.NextFlush ?? TimeSpan.MaxValue).TotalSeconds, nextEngage.TotalSeconds));
    }

    /// <summary>
    /// Sets a disposal unit so it is no longer moving towards flushing itself.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    public void Disengage(Entity<DisposalUnitComponent> ent)
    {
        ent.Comp.Engaged = false;

        if (GetContainedEntityCount(ent) == 0)
        {
            ent.Comp.NextFlush = null;
        }

        UpdateVisualState(ent);
        UpdateUI(ent);
        Dirty(ent);
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
    /// Primes a disposal unit to automatically flush sometime in the future.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    /// <param name="metadata">The disposal unit's metadata.</param>
    public void QueueAutomaticEngage(Entity<DisposalUnitComponent> ent, MetaDataComponent? metadata = null)
    {
        if (ent.Comp.Deleted ||
            !ent.Comp.AutomaticEngage ||
            !_power.IsPowered(ent.Owner) ||
            GetContainedEntityCount(ent) == 0)
        {
            return;
        }

        var pauseTime = _metaData.GetPauseTime(ent, metadata);
        var automaticTime = _timing.CurTime + ent.Comp.AutomaticEngageTime - pauseTime;
        var flushTime = TimeSpan.FromSeconds(Math.Min((ent.Comp.NextFlush ?? TimeSpan.MaxValue).TotalSeconds, automaticTime.TotalSeconds));

        ent.Comp.NextFlush = flushTime;
        Dirty(ent);
    }

    /// <summary>
    /// Toggles a disposal unit between 'engaged' and 'disengaged'.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
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

    /// <summary>
    /// All entities contained in a disposal unit.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    /// <returns>A copy of the disposal unit's ContainedEntities list.</returns>
    public IReadOnlyList<EntityUid> GetContainedEntities(Entity<DisposalUnitComponent> ent)
    {
        if (ent.Comp.Container == null)
            return new List<EntityUid>();

        return ent.Comp.Container.ContainedEntities.ToArray();
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
