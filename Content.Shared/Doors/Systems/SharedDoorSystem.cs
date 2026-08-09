using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Prying.Components;
using Content.Shared.Prying.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Content.Shared.Doors.Systems;

public abstract partial class SharedDoorSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] protected IGameTiming GameTiming = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] protected SharedPhysicsSystem PhysicsSystem = default!;
    [Dependency] private DamageableSystem _damageableSystem = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private SharedStunSystem _stunSystem = default!;
    [Dependency] protected TagSystem Tags = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] private EntityLookupSystem _entityLookup = default!;
    [Dependency] protected SharedAppearanceSystem AppearanceSystem = default!;
    [Dependency] private OccluderSystem _occluder = default!;
    [Dependency] private AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private PryingSystem _pryingSystem = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] private SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private EntityQuery<DoorComponent> _doorQuery = default!;
    [Dependency] private EntityQuery<FixturesComponent> _fixturesQuery = default!;
    [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery = default!;

    public static readonly ProtoId<TagPrototype> DoorBumpTag = "DoorBumpOpener";

    /// <summary>
    ///     A set of doors that are currently opening, closing, or just queued to open/close after some delay.
    /// </summary>
    private readonly HashSet<Entity<DoorComponent>> _activeDoors = new();
    private readonly HashSet<EntityUid> _tempSet = new();

    public override void Initialize()
    {
        base.Initialize();

        InitializeBolts();

        SubscribeLocalEvent<DoorComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<DoorComponent, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<DoorComponent, AfterAutoHandleStateEvent>(OnHandleState);

        SubscribeLocalEvent<DoorComponent, ActivateInWorldEvent>(OnActivate);

        SubscribeLocalEvent<DoorComponent, StartCollideEvent>(HandleCollide);
        SubscribeLocalEvent<DoorComponent, PreventCollideEvent>(PreventCollision);
        SubscribeLocalEvent<DoorComponent, BeforePryEvent>(OnBeforePry);
        SubscribeLocalEvent<DoorComponent, PriedEvent>(OnAfterPry);
        SubscribeLocalEvent<DoorComponent, WeldableAttemptEvent>(OnWeldAttempt);
        SubscribeLocalEvent<DoorComponent, WeldableChangedEvent>(OnWeldChanged);
        SubscribeLocalEvent<DoorComponent, GetPryTimeModifierEvent>(OnPryTimeModifier);
        SubscribeLocalEvent<DoorComponent, GotEmaggedEvent>(OnEmagged);
    }

    protected virtual void OnComponentInit(Entity<DoorComponent> ent, ref ComponentInit args)
    {
        var door = ent.Comp;
        if (door.NextStateChange != null)
            _activeDoors.Add(ent);
        else
        {
            // Make sure doors are not perpetually stuck opening or closing.
            if (door.State == DoorState.Opening)
            {
                // force to open.
                door.State = DoorState.Open;
                door.Partial = false;
            }
            if (door.State == DoorState.Closing)
            {
                // force to closed.
                door.State = DoorState.Closed;
                door.Partial = false;
            }
        }

        // should this door have collision and the like enabled?
        var collidable = door.State == DoorState.Closed
            || door.State == DoorState.Closing && door.Partial
            || door.State == DoorState.Opening && !door.Partial;

        SetCollidable(ent, collidable, door);
        AppearanceSystem.SetData(ent, DoorVisuals.State, door.State);
    }

    private void OnRemove(Entity<DoorComponent> door, ref ComponentRemove args)
    {
        _activeDoors.Remove(door);
    }

    private void OnEmagged(EntityUid uid, DoorComponent door, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Access))
            return;

        if (!TryComp<AirlockComponent>(uid, out var airlock))
            return;

        if (!airlock.Powered)
            return;

        if (door.State != DoorState.Closed)
            return;

        if (!SetState(uid, DoorState.Emagging, door))
            return;

        args.Repeatable = true;
        args.Handled = true;
    }

    #region StateManagement
    private void OnHandleState(Entity<DoorComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var door = ent.Comp;
        if (door.NextStateChange == null)
            _activeDoors.Remove(ent);
        else
            _activeDoors.Add(ent);

        RaiseLocalEvent(ent, new DoorStateChangedEvent(door.State));
    }

    public bool SetState(EntityUid uid, DoorState state, DoorComponent? door = null)
    {
        if (!Resolve(uid, ref door))
            return false;

        // If no change, return to avoid firing a new DoorStateChangedEvent.
        if (state == door.State)
            return false;

        switch (state)
        {
            case DoorState.Opening:
                _activeDoors.Add((uid, door));
                door.NextStateChange = GameTiming.CurTime + door.OpenTimeOne;
                break;

            case DoorState.Closing:
                _activeDoors.Add((uid, door));
                door.NextStateChange = GameTiming.CurTime + door.CloseTimeOne;
                break;

            case DoorState.Denying:
                _activeDoors.Add((uid, door));
                door.NextStateChange = GameTiming.CurTime + door.DenyDuration;
                break;

            case DoorState.Emagging:
                _activeDoors.Add((uid, door));
                door.NextStateChange = GameTiming.CurTime + door.EmagDuration;
                break;

            case DoorState.Open:
                door.Partial = false;
                if (door.NextStateChange == null)
                    _activeDoors.Remove((uid, door));
                break;
            case DoorState.Closed:
                // May want to keep the door around to re-check for opening if we got a contact during closing.
                door.Partial = false;
                break;
        }

        door.State = state;
        Dirty(uid, door);
        RaiseLocalEvent(uid, new DoorStateChangedEvent(state));

        AppearanceSystem.SetData(uid, DoorVisuals.State, door.State);
        return true;
    }

    #endregion

    #region Interactions
    protected void OnActivate(EntityUid uid, DoorComponent door, ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex || !door.ClickOpen)
            return;

        if (!TryToggleDoor(uid, door, args.User, predicted: true))
            _pryingSystem.TryPry(uid, args.User, out _);

        args.Handled = true;
    }

    private void OnPryTimeModifier(EntityUid uid, DoorComponent door, ref GetPryTimeModifierEvent args)
    {
        args.BaseTime = door.PryTime;
    }

    private void OnBeforePry(EntityUid uid, DoorComponent door, ref BeforePryEvent args)
    {
        if (door.State == DoorState.Welded || !door.CanPry)
            args.Cancelled = true;
    }

    /// <summary>
    ///     Open or close a door after it has been successfully pried.
    /// </summary>
    private void OnAfterPry(EntityUid uid, DoorComponent door, ref PriedEvent args)
    {
        if (door.State == DoorState.Closed)
        {
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User)} pried {ToPrettyString(uid)} open");
            StartOpening(uid, door, args.User, true);
        }
        else if (door.State == DoorState.Open)
        {
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User)} pried {ToPrettyString(uid)} closed");
            StartClosing(uid, door, args.User, true);
        }
    }

    private void OnWeldAttempt(EntityUid uid, DoorComponent component, WeldableAttemptEvent args)
    {
        if (component.CurrentlyCrushing.Count > 0)
        {
            args.Cancel();
            return;
        }
        if (component.State != DoorState.Closed && component.State != DoorState.Welded)
        {
            args.Cancel();
        }
    }

    private void OnWeldChanged(EntityUid uid, DoorComponent component, ref WeldableChangedEvent args)
    {
        if (component.State == DoorState.Closed)
            SetState(uid, DoorState.Welded, component);
        else if (component.State == DoorState.Welded)
            SetState(uid, DoorState.Closed, component);
    }

    /// <summary>
    ///     Update the door state/visuals and play an access denied sound when a user without access interacts with the
    ///     door.
    /// </summary>
    public void Deny(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool predicted = false)
    {
        if (!Resolve(uid, ref door))
            return;

        if (door.State != DoorState.Closed)
            return;

        // might not be able to deny without power or some other blocker.
        var ev = new BeforeDoorDeniedEvent();
        RaiseLocalEvent(uid, ev);
        if (ev.Cancelled)
            return;

        if (!SetState(uid, DoorState.Denying, door))
            return;

        var audioParams = door.DenySound?.Params ?? AudioParams.Default;
        audioParams = audioParams.AddVolume(-3);

        if (predicted)
            Audio.PlayPredicted(door.DenySound, uid, user, audioParams);
        else if (_net.IsServer)
            Audio.PlayPvs(door.DenySound, uid, audioParams);
    }

    public bool TryToggleDoor(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool predicted = false)
    {
        if (!Resolve(uid, ref door))
            return false;

        if (door.State is DoorState.Closed or DoorState.Denying)
        {
            return TryOpen(uid, door, user, predicted, quiet: door.State == DoorState.Denying);
        }

        if (door.State == DoorState.Open)
        {
            return TryClose(uid, door, user, predicted);
        }

        return false;
    }
    #endregion

    #region Opening
    public bool TryOpen(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool predicted = false, bool quiet = false)
    {
        if (!Resolve(uid, ref door))
            return false;

        if (!CanOpen(uid, door, user, quiet))
            return false;

        StartOpening(uid, door, user, predicted);

        return true;
    }

    public bool CanOpen(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool quiet = true)
    {
        if (!Resolve(uid, ref door))
            return false;

        if (door.State == DoorState.Welded)
            return false;

        if (Paused(uid))
            return false;

        var ev = new BeforeDoorOpenedEvent() { User = user };
        RaiseLocalEvent(uid, ev);
        if (ev.Cancelled)
            return false;

        if (!HasAccess(uid, user, door))
        {
            if (!quiet)
                Deny(uid, door, user, predicted: true);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Immediately start opening a door
    /// </summary>
    /// <param name="uid"> The uid of the door</param>
    /// <param name="door"> The doorcomponent of the door</param>
    /// <param name="user"> The user (if any) opening the door</param>
    /// <param name="predicted">Whether the interaction would have been
    /// predicted. See comments in the PlaySound method on the Server system for details</param>
    public void StartOpening(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool predicted = false)
    {
        if (!Resolve(uid, ref door))
            return;

        var lastState = door.State;

        if (!SetState(uid, DoorState.Opening, door))
            return;

        var audioParams = door.OpenSound?.Params ?? AudioParams.Default;
        audioParams = audioParams.AddVolume(-5);
        if (predicted)
            Audio.PlayPredicted(door.OpenSound, uid, user, audioParams);
        else if (_net.IsServer)
            Audio.PlayPvs(door.OpenSound, uid, audioParams);

        if (lastState == DoorState.Emagging && TryComp<DoorBoltComponent>(uid, out var doorBoltComponent))
            SetBoltsDown((uid, doorBoltComponent), true, user, true);
    }

    /// <summary>
    /// Called when the door is partially opened. The door becomes transparent and stops colliding with entities.
    /// </summary>
    public void OnPartialOpen(EntityUid uid, DoorComponent? door = null)
    {
        if (!Resolve(uid, ref door))
            return;

        SetCollidable(uid, false, door);
        door.Partial = true;
        door.NextStateChange = GameTiming.CurTime + door.OpenTimeTwo;
        _activeDoors.Add((uid, door));
        Dirty(uid, door);

    }

    /// <summary>
    /// Opens and then bolts a door.
    /// Different from emagging this does not remove the access reader, so it can be repaired by simply unbolting the door.
    /// </summary>
    public bool TryOpenAndBolt(EntityUid uid, DoorComponent? door = null, AirlockComponent? airlock = null)
    {
        if (!Resolve(uid, ref door, ref airlock))
            return false;

        if (IsBolted(uid) || !airlock.Powered || door.State != DoorState.Closed)
        {
            return false;
        }

        SetState(uid, DoorState.Emagging, door);

        return true;
    }
    #endregion

    #region Closing
    public bool TryClose(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool predicted = false)
    {
        if (!Resolve(uid, ref door))
            return false;

        if (!CanClose(uid, door, user))
            return false;

        StartClosing(uid, door, user, predicted);
        return true;
    }

    /// <summary>
    /// Immediately start closing a door
    /// </summary>
    /// <param name="uid"> The uid of the door</param>
    /// <param name="door"> The doorcomponent of the door</param>
    /// <param name="user"> The user (if any) opening the door</param>
    public bool CanClose(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool partial = false)
    {
        if (!Resolve(uid, ref door))
            return false;

        // since both closing/closed and welded are door states, we need to prevent 'closing'
        // a welded door or else there will be weird state bugs
        if (door.State is DoorState.Welded or DoorState.Closed)
            return false;

        if (Paused(uid))
            return false;

        var ev = new BeforeDoorClosedEvent(door.PerformCollisionCheck, partial);
        RaiseLocalEvent(uid, ev);
        if (ev.Cancelled)
            return false;

        if (!HasAccess(uid, user, door))
            return false;

        return !ev.PerformCollisionCheck || !AnyColliding(uid, door);
    }

    public void StartClosing(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool predicted = false)
    {
        if (!Resolve(uid, ref door))
            return;

        if (!SetState(uid, DoorState.Closing, door))
            return;

        var audioParams = door.CloseSound?.Params ?? AudioParams.Default;
        audioParams = audioParams.AddVolume(-5);

        if (predicted)
            Audio.PlayPredicted(door.CloseSound, uid, user, audioParams);
        else if (_net.IsServer)
            Audio.PlayPvs(door.CloseSound, uid, audioParams);
    }

    /// <summary>
    /// Called when the door is partially closed. This is when the door becomes "solid". If this process fails (e.g., a
    /// mob entered the door as it was closing), then this returns false. Otherwise, returns true;
    /// </summary>
    public bool OnPartialClose(EntityUid uid, DoorComponent? door = null, PhysicsComponent? physics = null)
    {
        if (!Resolve(uid, ref door, ref physics))
            return false;

        // Make sure no entity walked into the airlock when it started closing.
        if (!CanClose(uid, door, partial: true))
        {
            ReversePartialClose(uid, door, physics);
            return false;
        }

        door.Partial = true;
        door.NextStateChange = GameTiming.CurTime + door.CloseTimeTwo;
        Dirty(uid, door);
        SetCollidable(uid, true, door, physics);
        _activeDoors.Add((uid, door));

        // Crush any entities. Note that we don't check airlock safety here. This should have been checked before
        // the door closed.
        Crush(uid, door, physics);
        return true;
    }

    /// <summary>
    /// Reverses a door that reached the solid part of closing but found something in the way.
    /// </summary>
    private void ReversePartialClose(EntityUid uid, DoorComponent door, PhysicsComponent physics)
    {
        // The door never became solid, so this is the second half of an opening transition, not a fully-open state.
        // Sending Open immediately makes clients stop the closing animation and start a full opening animation while
        // the server queues another transition almost immediately.
        SetCollidable(uid, false, door, physics);
        door.Partial = true;
        door.RetryCloseAfterOpening = true;

        if (door.State != DoorState.Opening)
            SetState(uid, DoorState.Opening, door);

        door.NextStateChange = GameTiming.CurTime + door.OpenTimeTwo;
        _activeDoors.Add((uid, door));
        Dirty(uid, door);
    }
    #endregion

    #region Collisions

    protected virtual void SetCollidable(
        EntityUid uid,
        bool collidable,
        DoorComponent? door = null,
        PhysicsComponent? physics = null,
        OccluderComponent? occluder = null)
    {
        if (!Resolve(uid, ref door))
            return;

        if (Resolve(uid, ref physics, false))
            PhysicsSystem.SetCanCollide(uid, collidable, body: physics);

        if (!collidable)
            door.CurrentlyCrushing.Clear();

        if (door.Occludes)
            _occluder.SetEnabled(uid, collidable, occluder);
    }

    /// <summary>
    /// Crushes everyone colliding with the door.
    /// </summary>
    public void Crush(EntityUid uid, DoorComponent? door = null, PhysicsComponent? physics = null)
    {
        if (!Resolve(uid, ref door))
            return;

        if (!door.CanCrush)
            return;

        // Find entities and apply crushing effects
        var stunTime = door.DoorStunTime + door.OpenTimeOne;
        _tempSet.Clear();
        GetColliding(uid, _tempSet, door, physics);
        foreach (var entity in _tempSet)
        {
            door.CurrentlyCrushing.Add(entity);
            if (door.CrushDamage != null)
                _damageableSystem.TryChangeDamage(entity, door.CrushDamage, origin: uid);

            _stunSystem.TryUpdateParalyzeDuration(entity, stunTime);
        }
        _tempSet.Clear();

        if (door.CurrentlyCrushing.Count == 0)
            return;

        // queue the door to open so that the player is no longer stunned once it has FINISHED opening.
        door.NextStateChange = GameTiming.CurTime + door.DoorStunTime;
        door.Partial = false;
        Dirty(uid, door);
    }

    /// <summary>
    /// Checks whether any entity is colliding with the door while it is trying to close.
    /// </summary>
    public bool AnyColliding(
        EntityUid uid,
        DoorComponent? door = null,
        PhysicsComponent? physics = null,
        FixturesComponent? fixtures = null)
    {
        if (!_doorQuery.Resolve(uid, ref door) ||
            !_physicsQuery.Resolve(uid, ref physics) ||
            !_fixturesQuery.Resolve(uid, ref fixtures))
        {
            return false;
        }

        return FindColliding(uid, door, physics, fixtures, null);
    }

    /// <summary>
    /// Adds entities colliding with the door while it is trying to close.
    /// </summary>
    public void GetColliding(
        EntityUid uid,
        HashSet<EntityUid> colliding,
        DoorComponent? door = null,
        PhysicsComponent? physics = null,
        FixturesComponent? fixtures = null)
    {
        if (!_doorQuery.Resolve(uid, ref door) ||
            !_physicsQuery.Resolve(uid, ref physics) ||
            !_fixturesQuery.Resolve(uid, ref fixtures))
        {
            return;
        }

        FindColliding(uid, door, physics, fixtures, colliding);
    }

    private bool FindColliding(
        EntityUid uid,
        DoorComponent door,
        PhysicsComponent physics,
        FixturesComponent fixtures,
        HashSet<EntityUid>? colliding)
    {
        var xform = Transform(uid);
        var doorTransform = PhysicsSystem.GetPhysicsTransform(uid, xform);
        var state = new DoorCollisionQueryState(uid, physics, colliding);
        var callback = new DoorCollisionQueryCallback();

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            var query = GetDoorQuery(fixture);

            for (var i = 0; i < fixture.Shape.ChildCount; i++)
            {
                if (!_entityLookup.ForEachFixtureIntersecting(
                    xform.MapID,
                    fixture.Shape,
                    i,
                    doorTransform,
                    ref state,
                    callback,
                    query))
                {
                    return true;
                }
            }
        }

        return state.Found;
    }

    private static FixtureQueryArgs GetDoorQuery(Fixture fixture)
    {
        return new FixtureQueryArgs(
            new QueryFilter
            {
                LayerBits = fixture.CollisionLayer,
                MaskBits = fixture.CollisionMask,
                Flags = QueryFlags.Dynamic | QueryFlags.Static,
            },
            ignoreShapeSkin: true);
    }

    private struct DoorCollisionQueryState
    {
        public readonly EntityUid Door;
        public readonly PhysicsComponent Physics;
        public readonly HashSet<EntityUid>? Colliding;
        public bool Found;

        public DoorCollisionQueryState(EntityUid door, PhysicsComponent physics, HashSet<EntityUid>? colliding)
        {
            Door = door;
            Physics = physics;
            Colliding = colliding;
            Found = false;
        }
    }

    private readonly struct DoorCollisionQueryCallback : IFixtureQueryCallback<DoorCollisionQueryState>
    {
        public bool Invoke(ref DoorCollisionQueryState state, in FixtureProxy fixture)
        {
            if (fixture.Entity == state.Door || fixture.Body == state.Physics)
                return true;

            state.Found = true;

            if (state.Colliding == null)
                return false;

            state.Colliding.Add(fixture.Entity);
            return true;
        }
    }

    private void PreventCollision(EntityUid uid, DoorComponent component, ref PreventCollideEvent args)
    {
        if (component.CurrentlyCrushing.Contains(args.OtherEntity))
        {
            args.Cancelled = true;
        }
    }

    /// <summary>
    ///     Open a door if a player or door-bumper (PDA, ID-card) collide with the door. Sadly, bullets no longer
    ///     generate "access denied" sounds as you fire at a door.
    /// </summary>
    private void HandleCollide(EntityUid uid, DoorComponent door, ref StartCollideEvent args)
    {
        if (!door.BumpOpen)
            return;

        if (door.State is not (DoorState.Closed or DoorState.Denying))
            return;

        var otherUid = args.OtherEntity;

        if (Tags.HasTag(otherUid, DoorBumpTag))
            TryOpen(uid, door, otherUid, quiet: door.State == DoorState.Denying, predicted: true);
    }
    #endregion

    #region Access

    /// <summary>
    ///     Does the user have the permissions required to open this door?
    /// </summary>
    public bool HasAccess(EntityUid uid, EntityUid? user = null, DoorComponent? door = null, AccessReaderComponent? access = null)
    {
        // TODO network AccessComponent for predicting doors

        // if there is no "user" we skip the access checks. Access is also ignored in some game-modes.
        if (user == null || AccessType == AccessTypes.AllowAll)
            return true;

        // If the door is on emergency access we skip the checks.
        if (TryComp<AirlockComponent>(uid, out var airlock) && airlock.EmergencyAccess)
            return true;

        // Anyone can click to open firelocks
        if (Resolve(uid, ref door) && door.State == DoorState.Closed &&
            TryComp<FirelockComponent>(uid, out var firelock))
            return true;

        if (!Resolve(uid, ref access, false))
            return true;

        var isExternal = access.AccessLists.Any(list => list.Contains("External"));

        return AccessType switch
        {
            // Some game modes modify access rules.
            AccessTypes.AllowAllIdExternal => !isExternal || _accessReaderSystem.IsAllowed(user.Value, uid, access),
            AccessTypes.AllowAllNoExternal => !isExternal,
            _ => _accessReaderSystem.IsAllowed(user.Value, uid, access)
        };
    }

    /// <summary>
    ///     Determines the base access behavior of all doors on the station.
    /// </summary>
    public AccessTypes AccessType = AccessTypes.Id;

    /// <summary>
    /// How door access should be handled.
    /// </summary>
    public enum AccessTypes
    {
        /// <summary> ID based door access. </summary>
        Id,
        /// <summary>
        /// Allows everyone to open doors, except external which airlocks are still handled with ID's
        /// </summary>
        AllowAllIdExternal,
        /// <summary>
        /// Allows everyone to open doors, except external airlocks which are never allowed, even if the user has
        /// ID access.
        /// </summary>
        AllowAllNoExternal,
        /// <summary> Allows everyone to open all doors. </summary>
        AllowAll
    }
    #endregion

    #region Updating
    /// <summary>
    ///     Schedule an open or closed door to progress to the next state after some time.
    /// </summary>
    /// <remarks>
    ///     If the requested delay is null or non-positive, this will make the door stay open or closed indefinitely.
    /// </remarks>
    public void SetNextStateChange(EntityUid uid, TimeSpan? delay, DoorComponent? door = null)
    {
        if (!Resolve(uid, ref door, false))
            return;

        // If the door is not currently just open or closed, it is busy doing something else (or welded shut). So in
        // that case we do nothing.
        if (door.State != DoorState.Open && door.State != DoorState.Closed)
            return;

        // Is this trying to prevent an update? (e.g., cancel an auto-close)
        if (delay == null || delay.Value <= TimeSpan.Zero)
        {
            var hadNextStateChange = door.NextStateChange != null;
            door.NextStateChange = null;

            if (hadNextStateChange)
                Dirty(uid, door);

            _activeDoors.Remove((uid, door));
            return;
        }

        door.NextStateChange = GameTiming.CurTime + delay.Value;
        Dirty(uid, door);

        _activeDoors.Add((uid, door));
    }

    protected void CheckDoorBump(Entity<DoorComponent, PhysicsComponent> ent)
    {
        var (uid, door, physics) = ent;
        if (door.BumpOpen)
        {
            foreach (var other in PhysicsSystem.GetContactingEntities(uid, physics))
            {
                if (Tags.HasTag(other, DoorBumpTag) && TryOpen(uid, door, other, quiet: true))
                    break;
            }
        }
    }

    /// <summary>
    ///     Iterate over active doors and progress them to the next state if they need to be updated.
    /// </summary>
    public override void Update(float frameTime)
    {
        var time = GameTiming.CurTime;

        foreach (var ent in _activeDoors.ToList())
        {
            var door = ent.Comp;
            if (door.Deleted || door.NextStateChange == null)
            {
                _activeDoors.Remove(ent);
                continue;
            }

            if (Paused(ent))
                continue;

            if (door.NextStateChange.Value < time)
                NextState(ent, time);

            if (door.State == DoorState.Closed &&
                TryComp<PhysicsComponent>(ent, out var doorBody))
            {
                // If something bumped into us during closing then start to re-open, otherwise, remove it from active.
                _activeDoors.Remove(ent);
                CheckDoorBump((ent, door, doorBody));
            }
        }
    }

    /// <summary>
    ///     Makes a door proceed to the next state (if applicable).
    /// </summary>
    private void NextState(Entity<DoorComponent> ent, TimeSpan time)
    {
        var door = ent.Comp;
        door.NextStateChange = null;

        if (door.CurrentlyCrushing.Count > 0 && door.State != DoorState.Opening)
        {
            // This is a closed door that is crushing people and needs to auto-open. Note that we don't check "can open"
            // here. The door never actually finished closing and we don't want people to get stuck inside of doors.
            StartOpening(ent, door);
            return;
        }

        switch (door.State)
        {
            case DoorState.Opening:
                // Either fully or partially open this door.
                if (door.Partial)
                {
                    var retryClose = door.RetryCloseAfterOpening;
                    door.RetryCloseAfterOpening = false;
                    SetState(ent, DoorState.Open, door);
                    if (retryClose)
                        QueueCloseRetryAfterOpening(ent, door, time);
                }
                else
                {
                    OnPartialOpen(ent, door);
                }

                break;

            case DoorState.Closing:
                // Either fully or partially close this door.
                if (door.Partial)
                    SetState(ent, DoorState.Closed, door);
                else
                    OnPartialClose(ent, door);

                break;

            case DoorState.Denying:
                // Finish denying entry and return to the closed state.
                SetState(ent, DoorState.Closed, door);
                break;

            case DoorState.Emagging:
                StartOpening(ent, door);
                break;

            case DoorState.Open:
                // This door is open, and queued for an auto-close.
                if (!TryClose(ent, door))
                {
                    // The door failed to close (blocked?). Try again in one second.
                    door.NextStateChange = time + TimeSpan.FromSeconds(1);
                    Dirty(ent, door);
                }
                break;

            case DoorState.Welded:
                // A welded door? This should never have been active in the first place.
                Log.Error($"Welded door was in the list of active doors. Door: {ToPrettyString(ent)}");
                break;
        }
    }

    private void QueueCloseRetryAfterOpening(EntityUid uid, DoorComponent door, TimeSpan time)
    {
        var delay = door.OpeningAnimationTime - door.OpenTimeTwo;

        if (delay > TimeSpan.Zero)
        {
            SetNextStateChange(uid, delay, door);
            return;
        }

        if (TryClose(uid, door))
            return;

        door.NextStateChange = time + TimeSpan.FromSeconds(1);
        _activeDoors.Add((uid, door));
        Dirty(uid, door);
    }
    #endregion
}
