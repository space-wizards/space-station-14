using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Prying.Components;
using Content.Shared.Prying.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Map.Components;

namespace Content.Shared.Doors.Systems;

public abstract partial class SharedDoorSystem : EntitySystem
{

    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly OccluderSystem _occluder = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly PryingSystem _pryingSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;

    [ValidatePrototypeId<TagPrototype>] private const string DoorBumpTag = "DoorBumpOpener";

    /// <summary>
    ///     Determines the base access behavior of all doors on the station.
    /// </summary>
    private const AccessTypes AccessType = AccessTypes.Id;

    /// <summary>
    ///     A set of doors that are currently opening, closing, or just queued to open/close after some delay.
    /// </summary>
    private readonly HashSet<Entity<DoorComponent>> _activeDoors = [];

    private readonly HashSet<Entity<PhysicsComponent>> _doorIntersecting = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoorComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<DoorComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<DoorComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<DoorComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<DoorComponent, BeforePryEvent>(OnBeforePry);
        SubscribeLocalEvent<DoorComponent, PriedEvent>(OnAfterPry);
        SubscribeLocalEvent<DoorComponent, WeldableAttemptEvent>(OnWeldAttempt);
        SubscribeLocalEvent<DoorComponent, WeldableChangedEvent>(OnWeldChanged);
        SubscribeLocalEvent<DoorComponent, GetPryTimeModifierEvent>(OnPryTimeModifier);
        SubscribeLocalEvent<DoorComponent, GotEmaggedEvent>(OnEmagged);

        InitializeCollision();
        InitializeBolts();
        InitializeAirlock();
        InitializeAlarm();
    }

    /// <summary>
    ///     Iterate over active doors and progress them to the next state if they need to be updated.
    /// </summary>
    public override void Update(float frameTime)
    {
        var time = _gameTiming.CurTime;

        foreach (var door in _activeDoors.ToList())
        {
            if (door.Comp.Deleted || door.Comp.NextStateChange == null)
            {
                _activeDoors.Remove(door);

                continue;
            }

            if (Paused(door))
                continue;

            if (door.Comp.NextStateChange.Value < time)
                NextState(door);

            if (door.Comp.State != DoorState.Closed || !TryComp<PhysicsComponent>(door, out var doorBody))
                continue;

            // If something bumped into us during closing then start to re-open, otherwise, remove it from active.
            _activeDoors.Remove(door);
            CheckDoorBump((door, door, doorBody));
        }
    }

    protected virtual void OnComponentInit(Entity<DoorComponent> door, ref ComponentInit args)
    {
        if (door.Comp.NextStateChange == null)
        {
            switch (door.Comp.State)
            {
                // Make sure doors are not perpetually stuck opening or closing.
                case DoorState.AttemptingOpenBySelf or DoorState.AttemptingOpenByPrying or DoorState.Opening:
                    door.Comp.State = DoorState.Open;

                    break;
                case DoorState.AttemptingCloseBySelf or DoorState.AttemptingCloseByPrying or DoorState.Closing:
                    door.Comp.State = DoorState.Closed;

                    break;
            }
        }
        else
            _activeDoors.Add(door);

        SetCollidable(door, IsDoorCollidable(door));

        _appearance.SetData(door, DoorVisuals.State, door.Comp.State);
    }

    private void OnRemove(Entity<DoorComponent> door, ref ComponentRemove args)
    {
        _activeDoors.Remove(door);
    }

    private void OnEmagged(Entity<DoorComponent> entity, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Access))
            return;

        if (!TryComp<AirlockComponent>(entity, out var airlock)
            || IsBolted(entity)
            || !airlock.Powered
            || entity.Comp.State is not DoorState.Closed
            || !SetState(entity, DoorState.Emagging))
            return;

        args.Repeatable = true;
        args.Handled = true;
    }

    #region StateManagement

    private void OnHandleState(Entity<DoorComponent> door, ref AfterAutoHandleStateEvent args)
    {
        if (door.Comp.NextStateChange == null)
            _activeDoors.Remove(door);
        else
            _activeDoors.Add(door);
    }

    protected bool SetState(Entity<DoorComponent> door, DoorState state)
    {
        // If no change, return to avoid firing a new DoorStateChangedEvent.
        if (state == door.Comp.State)
            return false;

        switch (state)
        {
            case DoorState.AttemptingOpenBySelf or DoorState.AttemptingOpenByPrying:
                _activeDoors.Add((door, door));
                door.Comp.NextStateChange = _gameTiming.CurTime + door.Comp.OpenTimeOne;

                break;
            case DoorState.AttemptingCloseBySelf or DoorState.AttemptingCloseByPrying:
                _activeDoors.Add((door, door));
                door.Comp.NextStateChange = _gameTiming.CurTime + door.Comp.CloseTimeOne;

                break;
            case DoorState.Denying:
                _activeDoors.Add((door, door));
                door.Comp.NextStateChange = _gameTiming.CurTime + door.Comp.DenyDuration;

                break;
            case DoorState.Emagging:
                _activeDoors.Add((door, door));
                door.Comp.NextStateChange = _gameTiming.CurTime + door.Comp.EmagDuration;

                break;
            case DoorState.Open:
                if (door.Comp.NextStateChange == null)
                    _activeDoors.Remove((door, door));

                break;
        }

        var oldState = door.Comp.State;
        door.Comp.State = state;

        _appearance.SetData(door, DoorVisuals.State, door.Comp.State);

        Dirty(door);

        RaiseLocalEvent(door, new DoorStateChangedEvent(state, oldState));

        return true;
    }

    #endregion

    #region Interactions

    private void OnActivate(Entity<DoorComponent> door, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex || !door.Comp.ClickOpen)
            return;

        if (!TryToggleDoor(door, args.User, predicted: true))
            _pryingSystem.TryPry(door, args.User, out _);

        args.Handled = true;
    }

    private void OnPryTimeModifier(Entity<DoorComponent> door, ref GetPryTimeModifierEvent args)
    {
        args.BaseTime = door.Comp.PryTime;
    }

    private void OnBeforePry(Entity<DoorComponent> door, ref BeforePryEvent args)
    {
        if (door.Comp.State == DoorState.Welded || !door.Comp.CanPry)
            args.Cancelled = true;
    }

    /// <summary>
    ///     Open or close a door after it has been successfully pried.
    /// </summary>
    private void OnAfterPry(Entity<DoorComponent> door, ref PriedEvent args)
    {
        switch (door.Comp.State)
        {
            case DoorState.Closed:
                _adminLog.Add(LogType.Action,
                    LogImpact.Medium,
                    $"{ToPrettyString(args.User)} pried {ToPrettyString(door)} open");
                StartOpening(door, args.User, true, true);

                return;
            case DoorState.Open:
                _adminLog.Add(LogType.Action,
                    LogImpact.Medium,
                    $"{ToPrettyString(args.User)} pried {ToPrettyString(door)} closed");
                StartClosing(door, args.User, true, true);

                return;
        }
    }

    private void OnWeldAttempt(Entity<DoorComponent> component, ref WeldableAttemptEvent args)
    {
        if (component.Comp.CurrentlyCrushing.Count <= 0 &&
            component.Comp.State is DoorState.Closed or DoorState.Welded)
            return;

        args.Cancel();
    }

    private void OnWeldChanged(Entity<DoorComponent> door, ref WeldableChangedEvent args)
    {
        switch (door.Comp.State)
        {
            case DoorState.Closed:
                SetState(door, DoorState.Welded);

                return;
            case DoorState.Welded:
                SetState(door, DoorState.Closed);

                return;
        }
    }

    /// <summary>
    /// Update the door state/visuals and play the access denied SFX when a user without access interacts with the door.
    /// </summary>
    public void Deny(Entity<DoorComponent> door, EntityUid? user = null, bool predicted = false)
    {
        if (door.Comp.State != DoorState.Closed)
            return;

        // Might not be able to deny without power or some other blocker.
        var ev = new BeforeDoorDeniedEvent();
        RaiseLocalEvent(door, ev);

        if (ev.Cancelled)
            return;

        if (!SetState(door, DoorState.Denying))
            return;

        if (predicted)
            _audio.PlayPredicted(door.Comp.DenySound, door, user, AudioParams.Default.WithVolume(-3));
        else if (_net.IsServer)
            _audio.PlayPvs(door.Comp.DenySound, door, AudioParams.Default.WithVolume(-3));
    }

    public bool TryToggleDoor(Entity<DoorComponent> door, EntityUid? user = null, bool predicted = false)
    {
        switch (door.Comp.State)
        {
            case DoorState.Closed or DoorState.Denying:
                return TryOpen(door, user, predicted, quiet: door.Comp.State == DoorState.Denying);
            case DoorState.Open:
                return TryClose(door, user, predicted);
            default:
                return false;
        }
    }

    #endregion

    #region Opening

    public bool TryOpen(Entity<DoorComponent> door, EntityUid? user = null, bool predicted = false, bool quiet = false)
    {
        if (!CanOpen(door, user, quiet))
            return false;

        StartOpening(door, user, predicted);

        return true;
    }

    public bool CanOpen(Entity<DoorComponent> door, EntityUid? user = null, bool quiet = true)
    {
        if (door.Comp.State == DoorState.Welded)
            return false;

        var ev = new BeforeDoorOpenedEvent
        {
            User = user,
        };

        RaiseLocalEvent(door, ev);

        if (ev.Cancelled)
            return false;

        if (HasAccess(door, user))
            return true;

        if (!quiet)
            Deny(door, user, predicted: true);

        return false;
    }

    /// <summary>
    /// Start opening a door.
    /// </summary>
    /// <param name="door"> The door component of the door</param>
    /// <param name="user"> The user (if any) opening the door</param>
    /// <param name="predicted">Whether the interaction would have been
    /// predicted. See comments in the PlaySound method on the Server system for details</param>
    /// <param name="pried">Is the door being pried open?</param>
    public void StartOpening(Entity<DoorComponent> door,
        EntityUid? user = null,
        bool predicted = false,
        bool pried = false)
    {
        var lastState = door.Comp.State;

        if (!SetState(door, pried ? DoorState.AttemptingOpenByPrying : DoorState.AttemptingOpenBySelf))
            return;

        if (predicted)
            _audio.PlayPredicted(door.Comp.OpenSound, door, user, AudioParams.Default.WithVolume(-5));
        else if (_net.IsServer)
            _audio.PlayPvs(door.Comp.OpenSound, door, AudioParams.Default.WithVolume(-5));

        if (lastState is DoorState.Emagging && TryComp<DoorBoltComponent>(door, out var doorBoltComponent))
            SetBoltsDown((door, doorBoltComponent), !doorBoltComponent.BoltsDown, user, true);
    }

    /// <summary>
    /// Called when the door is partially opened. The door becomes transparent and stops colliding with entities.
    /// </summary>
    private void OnPartialOpen(Entity<DoorComponent> door)
    {
        SetCollidable(door, false);
        SetState(door, DoorState.Opening);

        door.Comp.NextStateChange = _gameTiming.CurTime + door.Comp.OpenTimeTwo;

        _activeDoors.Add((door, door));

        Dirty(door);
    }

    /// <summary>
    /// Opens and then bolts a door.
    /// Different from emagging this does not remove the access reader, so it can be repaired by simply unbolting the
    /// door.
    /// </summary>
    public void TryOpenAndBolt(Entity<DoorComponent> door, AirlockComponent? airlock = null)
    {
        if (!Resolve(door, ref airlock)
            || IsBolted(door)
            || !airlock.Powered
            || door.Comp.State is not DoorState.Closed)
            return;

        SetState(door, DoorState.Emagging);
    }

    #endregion

    #region Closing

    public bool TryClose(Entity<DoorComponent> door, EntityUid? user = null, bool predicted = false, bool pried = false)
    {
        if (!CanClose(door, user))
            return false;

        StartClosing(door, user, predicted, pried);

        return true;
    }

    /// <summary>
    /// Immediately start closing a door
    /// </summary>
    /// <param name="door"> The door component of the door</param>
    /// <param name="user"> The user (if any) closing the door</param>
    private bool CanClose(Entity<DoorComponent> door, EntityUid? user = null)
    {
        // Since both closing/closed and welded are door states, we need to prevent 'closing' a welded door or else
        // there will be weird state bugs.
        if (door.Comp.State is DoorState.Welded or DoorState.Closed)
            return false;

        var ev = new BeforeDoorClosedEvent(door.Comp.PerformCollisionCheck);
        RaiseLocalEvent(door, ev);

        if (ev.Cancelled)
            return false;

        if (!HasAccess(door, user))
            return false;

        return !ev.PerformCollisionCheck || !GetColliding(door).Any();
    }

    private void StartClosing(Entity<DoorComponent> door,
        EntityUid? user = null,
        bool predicted = false,
        bool isPried = false)
    {
        if (isPried)
        {
            if (!SetState(door, DoorState.AttemptingCloseByPrying))
                return;
        }
        else if (!SetState(door, DoorState.AttemptingCloseBySelf))
            return;

        if (predicted)
            _audio.PlayPredicted(door.Comp.CloseSound, door, user, AudioParams.Default.WithVolume(-5));
        else if (_net.IsServer)
            _audio.PlayPvs(door.Comp.CloseSound, door, AudioParams.Default.WithVolume(-5));
    }

    /// <summary>
    /// Called when the door is partially closed. This is when the door becomes "solid".
    /// </summary>
    private void CloseDoor(Entity<DoorComponent> door)
    {
        // Make sure no entity walked into the airlock when it started closing.
        if (!CanClose(door))
        {
            SetState(door, DoorState.Open);

            door.Comp.NextStateChange = _gameTiming.CurTime + door.Comp.OpenTimeTwo;

            return;
        }

        SetState(door, DoorState.Closing);
        SetCollidable(door, true);

        door.Comp.NextStateChange = _gameTiming.CurTime + door.Comp.CloseTimeTwo;

        _activeDoors.Add((door, door));

        // Crush any entities. Note that we don't check airlock safety here. This should have been checked before
        // the door closed.
        Crush(door);
    }

    #endregion

    #region Access

    /// <summary>
    /// Does the user have the permissions required to open this door?
    /// </summary>
    public bool HasAccess(Entity<DoorComponent> door, EntityUid? user = null, AccessReaderComponent? access = null)
    {
        // TODO network AccessComponent for predicting doors

        // if there is no "user" we skip the access checks.
        if (user == null || AccessType == AccessTypes.AllowAll)
            return true;

        // If the door is on emergency access we skip the checks.
        if (TryComp<AirlockComponent>(door, out var airlock) && airlock.EmergencyAccess)
            return true;

        // Anyone can click to open firelocks.
        if (door.Comp.State == DoorState.Closed && TryComp<FirelockComponent>(door, out _))
            return true;

        return !Resolve(door, ref access, false) || _accessReaderSystem.IsAllowed(user.Value, door, access);
    }

    /// <summary>
    /// How door access should be handled.
    /// </summary>
    public enum AccessTypes
    {
        /// <summary> ID based door access. </summary>
        Id,
        /// <summary> Allows everyone to open all doors. </summary>
        AllowAll,
    }

    #endregion

    #region Updating

    /// <summary>
    ///     Schedule an open or closed door to progress to the next state after some time.
    /// </summary>
    /// <remarks>
    ///     If the requested delay is null or non-positive, this will make the door stay open or closed indefinitely.
    /// </remarks>
    public void SetNextStateChange(Entity<DoorComponent> door, TimeSpan? delay)
    {
        // If the door is not currently just open or closed, it is busy doing something else (or welded shut). So in
        // that case we do nothing.
        if (door.Comp.State is not (DoorState.Open or DoorState.Closed))
            return;

        // Is this trying to prevent an update? (e.g., cancel an auto-close)
        if (delay == null || delay.Value <= TimeSpan.Zero)
        {
            door.Comp.NextStateChange = null;
            _activeDoors.Remove((door, door));

            return;
        }

        door.Comp.NextStateChange = _gameTiming.CurTime + delay.Value;
        Dirty(door);

        _activeDoors.Add((door, door));
    }

    private void CheckDoorBump(Entity<DoorComponent, PhysicsComponent> ent)
    {
        var (uid, door, physics) = ent;
        if (!door.BumpOpen)
            return;

        foreach (var other in _physics.GetContactingEntities(uid, physics))
        {
            if (_tag.HasTag(other, DoorBumpTag) && TryOpen(ent, other, quiet: true))
                break;
        }
    }

    /// <summary>
    ///     Makes a door proceed to the next state (if applicable).
    /// </summary>
    private void NextState(Entity<DoorComponent> door)
    {
        door.Comp.NextStateChange = null;

        if (door.Comp.CurrentlyCrushing.Count > 0)
            // This is a closed door that is crushing people and needs to auto-open. Note that we don't check "can open"
            // here. The door never actually finished closing and we don't want people to get stuck inside of doors.
            StartOpening(door, door);

        switch (door.Comp.State)
        {
            case DoorState.AttemptingOpenByPrying or DoorState.AttemptingOpenBySelf:
                OnPartialOpen(door);

                return;
            case DoorState.Opening:
                SetState(door, DoorState.Open);

                return;
            case DoorState.AttemptingCloseBySelf or DoorState.AttemptingCloseByPrying:
                CloseDoor(door);

                return;
            case DoorState.Closing or DoorState.Denying:
                SetState(door, DoorState.Closed);

                return;
            case DoorState.Emagging:
                StartOpening(door);
                return;

            case DoorState.Open:
                // This door is open, and queued for an auto-close.
                if (!TryClose(door))
                {
                    // The door failed to close (blocked?). Try again in one second.
                    door.Comp.NextStateChange = _gameTiming.CurTime + door.Comp.OpenTimeTwo;
                }

                return;
            case DoorState.Welded:
                // A welded door? This should never have been active in the first place.
                Log.Error($"Welded door was in the list of active doors. Door: {ToPrettyString(door)}");

                return;
        }
    }

    #endregion

}
