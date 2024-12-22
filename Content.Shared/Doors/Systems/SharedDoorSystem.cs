using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Physics;
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
using Robust.Shared.Map.Components;

namespace Content.Shared.Doors.Systems;

public abstract partial class SharedDoorSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly SharedPhysicsSystem PhysicsSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] protected readonly TagSystem Tags = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] protected readonly SharedAppearanceSystem AppearanceSystem = default!;
    [Dependency] private readonly OccluderSystem _occluder = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly PryingSystem _pryingSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;

    [ValidatePrototypeId<TagPrototype>]
    public const string DoorBumpTag = "DoorBumpOpener";

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
        SubscribeLocalEvent<DoorComponent, StartCollideEvent>(HandleCollide);
        SubscribeLocalEvent<DoorComponent, PreventCollideEvent>(PreventCollision);
        SubscribeLocalEvent<DoorComponent, BeforePryEvent>(OnBeforePry);
        SubscribeLocalEvent<DoorComponent, PriedEvent>(OnAfterPry);
        SubscribeLocalEvent<DoorComponent, WeldableAttemptEvent>(OnWeldAttempt);
        SubscribeLocalEvent<DoorComponent, WeldableChangedEvent>(OnWeldChanged);
        SubscribeLocalEvent<DoorComponent, GetPryTimeModifierEvent>(OnPryTimeModifier);
        SubscribeLocalEvent<DoorComponent, OnAttemptEmagEvent>(OnAttemptEmag);
        SubscribeLocalEvent<DoorComponent, GotEmaggedEvent>(OnEmagged);

        InitializeBolts();
    }

    protected virtual void OnComponentInit(Entity<DoorComponent> door, ref ComponentInit args)
    {
        if (door.Comp.NextStateChange == null)
        {
            switch (door.Comp.State)
            {
                // Make sure doors are not perpetually stuck opening or closing.
                case DoorState.AttemptingOpenBySelf:
                case DoorState.AttemptingOpenByPrying:
                case DoorState.OpeningInProgress:
                    // force to open.
                    door.Comp.State = DoorState.Open;
                    // door.Partial = false;
                    break;
                case DoorState.AttemptingCloseBySelf:
                case DoorState.AttemptingCloseByPrying:
                case DoorState.ClosingInProgress:
                    // force to closed.
                    door.Comp.State = DoorState.Closed;
                    // door.Partial = false;
                    break;
                case DoorState.Closed:
                case DoorState.Open:
                case DoorState.WeldedClosed:
                case DoorState.Denying:
                case DoorState.Emagging:
                default:
                    break;
            }
        }
        else
            _activeDoors.Add(door);

        SetCollidable(door, IsDoorCollidable(door));
        AppearanceSystem.SetData(door, DoorVisuals.State, door.Comp.State);
    }

    /// <summary>
    /// Should the door have collision (i.e. block transit)?
    /// </summary>
    /// <param name="door">The door in question</param>
    /// <returns>True if the door is closed or currently closing, and open if the door is open or currently opening.</returns>
    private static bool IsDoorCollidable(DoorComponent door)
    {
        switch (door.State)
        {
            case DoorState.Closed:
            case DoorState.AttemptingOpenBySelf:
            case DoorState.AttemptingOpenByPrying:
            case DoorState.ClosingInProgress:
            case DoorState.WeldedClosed:
            case DoorState.Denying:
            case DoorState.Emagging:
                return true;
            case DoorState.AttemptingCloseBySelf:
            case DoorState.AttemptingCloseByPrying:
            case DoorState.Open:
            case DoorState.OpeningInProgress:
            default:
                return false;
        }
    }

    private void OnRemove(Entity<DoorComponent> door, ref ComponentRemove args)
    {
        _activeDoors.Remove(door);
    }

    private void OnAttemptEmag(Entity<DoorComponent> door, ref OnAttemptEmagEvent args)
    {
        if (TryComp<AirlockComponent>(door, out var airlock) && !IsBolted(door) && airlock.Powered &&
            door.Comp.State == DoorState.Closed)
            return;

        args.Handled = true;
    }

    private void OnEmagged(Entity<DoorComponent> door, ref GotEmaggedEvent args)
    {
        if (!SetState(door, DoorState.Emagging))
            return;

        Audio.PlayPredicted(door.Comp.SparkSound, door, args.UserUid, AudioParams.Default.WithVolume(8));
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

    protected bool SetState(Entity<DoorComponent> door, DoorState state)
    {
        // If no change, return to avoid firing a new DoorStateChangedEvent.
        if (state == door.Comp.State)
            return false;

        switch (state)
        {
            case DoorState.AttemptingOpenBySelf:
            case DoorState.AttemptingOpenByPrying:
                _activeDoors.Add((door, door));
                door.Comp.NextStateChange = GameTiming.CurTime + door.Comp.OpenTimeOne;

                break;
            case DoorState.AttemptingCloseBySelf:
            case DoorState.AttemptingCloseByPrying:
                _activeDoors.Add((door, door));
                door.Comp.NextStateChange = GameTiming.CurTime + door.Comp.CloseTimeOne;

                break;
            case DoorState.Denying:
                _activeDoors.Add((door, door));
                door.Comp.NextStateChange = GameTiming.CurTime + door.Comp.DenyDuration;

                break;
            case DoorState.Emagging:
                _activeDoors.Add((door, door));
                door.Comp.NextStateChange = GameTiming.CurTime + door.Comp.EmagDuration;

                break;
            case DoorState.Open:
                if (door.Comp.NextStateChange == null)
                    _activeDoors.Remove((door, door));

                break;
            case DoorState.Closed:
            case DoorState.ClosingInProgress:
            case DoorState.OpeningInProgress:
            case DoorState.WeldedClosed:
            default:
                break;
        }

        door.Comp.State = state;
        Dirty(door);
        RaiseLocalEvent(door, new DoorStateChangedEvent(state));

        AppearanceSystem.SetData(door, DoorVisuals.State, door.Comp.State);

        return true;
    }

    #endregion

    #region Interactions

    protected void OnActivate(Entity<DoorComponent> door, ref ActivateInWorldEvent args)
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
        if (door.Comp.State == DoorState.WeldedClosed || !door.Comp.CanPry)
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
            case DoorState.AttemptingCloseBySelf:
            case DoorState.AttemptingOpenBySelf:
            case DoorState.WeldedClosed:
            case DoorState.Denying:
            case DoorState.Emagging:
            case DoorState.AttemptingCloseByPrying:
            case DoorState.ClosingInProgress:
            case DoorState.AttemptingOpenByPrying:
            case DoorState.OpeningInProgress:
            default:
                return;
        }
    }

    private void OnWeldAttempt(Entity<DoorComponent> component, ref WeldableAttemptEvent args)
    {
        if (component.Comp.CurrentlyCrushing.Count <= 0 &&
            component.Comp.State is DoorState.Closed or DoorState.WeldedClosed)
            return;

        args.Cancel();
    }

    private void OnWeldChanged(Entity<DoorComponent> door, ref WeldableChangedEvent args)
    {
        switch (door.Comp.State)
        {
            case DoorState.Closed:
                SetState(door, DoorState.WeldedClosed);

                return;
            case DoorState.WeldedClosed:
                SetState(door, DoorState.Closed);

                return;
            case DoorState.AttemptingCloseBySelf:
            case DoorState.AttemptingCloseByPrying:
            case DoorState.ClosingInProgress:
            case DoorState.Open:
            case DoorState.AttemptingOpenBySelf:
            case DoorState.AttemptingOpenByPrying:
            case DoorState.OpeningInProgress:
            case DoorState.Denying:
            case DoorState.Emagging:
            default:
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

        // might not be able to deny without power or some other blocker.
        var ev = new BeforeDoorDeniedEvent();
        RaiseLocalEvent(door, ev);

        if (ev.Cancelled)
            return;

        if (!SetState(door, DoorState.Denying))
            return;

        // TODO: Review if coping with prediction should be in Shared, this feels more like a Client/Server distinction.
        // One to discuss with @metalgearsloth
        if (predicted)
            Audio.PlayPredicted(door.Comp.DenySound, door, user, AudioParams.Default.WithVolume(-3));
        else if (_net.IsServer)
            Audio.PlayPvs(door.Comp.DenySound, door, AudioParams.Default.WithVolume(-3));
    }

    public bool TryToggleDoor(Entity<DoorComponent> door, EntityUid? user = null, bool predicted = false)
    {
        switch (door.Comp.State)
        {
            case DoorState.Closed:
            case DoorState.Denying:
                return TryOpen(door, user, predicted, quiet: door.Comp.State == DoorState.Denying);
            case DoorState.Open:
                return TryClose(door, user, predicted);
            case DoorState.AttemptingCloseBySelf:
            case DoorState.AttemptingCloseByPrying:
            case DoorState.ClosingInProgress:
            case DoorState.AttemptingOpenBySelf:
            case DoorState.AttemptingOpenByPrying:
            case DoorState.OpeningInProgress:
            case DoorState.WeldedClosed:
            case DoorState.Emagging:
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
        if (door.Comp.State == DoorState.WeldedClosed)
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
    /// Immediately start opening a door
    /// </summary>
    /// <param name="door"> The doorcomponent of the door</param>
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

        // TODO: Review if prediction code should logically be here and not in Client/Server.
        if (predicted)
            Audio.PlayPredicted(door.Comp.OpenSound, door, user, AudioParams.Default.WithVolume(-5));
        else if (_net.IsServer)
            Audio.PlayPvs(door.Comp.OpenSound, door, AudioParams.Default.WithVolume(-5));

        if (lastState == DoorState.Emagging && TryComp<DoorBoltComponent>(door, out var doorBoltComponent))
            SetBoltsDown((door, doorBoltComponent), !doorBoltComponent.BoltsDown, user, true);
    }

    /// <summary>
    /// Called when the door is partially opened. The door becomes transparent and stops colliding with entities.
    /// </summary>
    public void OnPartialOpen(Entity<DoorComponent> door)
    {
        SetCollidable(door, false);
        SetState(door, DoorState.OpeningInProgress);
        door.Comp.NextStateChange = GameTiming.CurTime + door.Comp.OpenTimeTwo;
        _activeDoors.Add((door, door));
        Dirty(door);
    }

    /// <summary>
    /// Opens and then bolts a door.
    /// Different from emagging this does not remove the access reader, so it can be repaired by simply unbolting the door.
    /// </summary>
    public void TryOpenAndBolt(Entity<DoorComponent> door, AirlockComponent? airlock = null)
    {
        if (!Resolve(door, ref airlock) || IsBolted(door) || !airlock.Powered || door.Comp.State != DoorState.Closed)
            return;

        SetState(door, DoorState.Emagging);
    }

    #endregion

    #region Closing

    public bool TryClose(Entity<DoorComponent> door, EntityUid? user = null, bool predicted = false)
    {
        if (!CanClose(door, user))
            return false;

        StartClosing(door, user, predicted);

        return true;
    }

    /// <summary>
    /// Immediately start closing a door
    /// </summary>
    /// <param name="door"> The doorcomponent of the door</param>
    /// <param name="user"> The user (if any) opening the door</param>
    public bool CanClose(Entity<DoorComponent> door, EntityUid? user = null)
    {
        // since both closing/closed and welded are door states, we need to prevent 'closing'
        // a welded door or else there will be weird state bugs
        if (door.Comp.State is DoorState.WeldedClosed or DoorState.Closed)
            return false;

        var ev = new BeforeDoorClosedEvent(door.Comp.PerformCollisionCheck);
        RaiseLocalEvent(door, ev);
        if (ev.Cancelled)
            return false;

        if (!HasAccess(door, user))
            return false;

        return !ev.PerformCollisionCheck || !GetColliding(door).Any();
    }

    public void StartClosing(Entity<DoorComponent> door,
        EntityUid? user = null,
        bool predicted = false,
        bool isPried = false)
    {
        if (!SetState(door, isPried ? DoorState.AttemptingCloseByPrying : DoorState.AttemptingCloseBySelf))
            return;

        if (predicted)
            Audio.PlayPredicted(door.Comp.CloseSound, door, user, AudioParams.Default.WithVolume(-5));
        else if (_net.IsServer)
            Audio.PlayPvs(door.Comp.CloseSound, door, AudioParams.Default.WithVolume(-5));
    }

    /// <summary>
    /// Called when the door is partially closed. This is when the door becomes "solid". If this process fails (e.g., a
    /// mob entered the door as it was closing), then this returns false. Otherwise, returns true;
    /// </summary>
    public bool OnPartialClose(Entity<DoorComponent> door, PhysicsComponent? physics = null)
    {
        if (!Resolve(door, ref physics))
            return false;

        // Make sure no entity walked into the airlock when it started closing.
        if (!CanClose(door, door))
        {
            door.Comp.NextStateChange = GameTiming.CurTime + door.Comp.OpenTimeTwo;
            door.Comp.State = DoorState.Open;
            AppearanceSystem.SetData(door, DoorVisuals.State, DoorState.Open);
            Dirty(door);

            return false;
        }

        SetState(door, DoorState.ClosingInProgress);
        SetCollidable(door, true, physics);
        door.Comp.NextStateChange = GameTiming.CurTime + door.Comp.CloseTimeTwo;
        Dirty(door);
        _activeDoors.Add((door, door));

        // Crush any entities. Note that we don't check airlock safety here. This should have been checked before
        // the door closed.
        Crush(door, physics);

        return true;
    }

    #endregion

    #region Collisions

    protected virtual void SetCollidable(Entity<DoorComponent> door,
        bool collidable,
        PhysicsComponent? physics = null,
        OccluderComponent? occluder = null)
    {
        if (Resolve(door, ref physics, false))
            PhysicsSystem.SetCanCollide(door, collidable, body: physics);

        if (!collidable)
            door.Comp.CurrentlyCrushing.Clear();

        if (door.Comp.Occludes)
            _occluder.SetEnabled(door, collidable, occluder);
    }

    /// <summary>
    /// Crushes everyone colliding with us by more than <see cref="IntersectPercentage"/>%.
    /// </summary>
    public void Crush(Entity<DoorComponent> door, PhysicsComponent? physics = null)
    {
        if (!door.Comp.CanCrush)
            return;

        // Find entities and apply crushing effects
        var stunTime = door.Comp.DoorStunTime + door.Comp.OpenTimeOne;
        foreach (var entity in GetColliding(door, physics))
        {
            door.Comp.CurrentlyCrushing.Add(entity);
            if (door.Comp.CrushDamage != null)
                _damageableSystem.TryChangeDamage(entity, door.Comp.CrushDamage, origin: door);

            _stunSystem.TryParalyze(entity, stunTime, true);
        }

        if (door.Comp.CurrentlyCrushing.Count == 0)
            return;

        // queue the door to open so that the player is no longer stunned once it has FINISHED opening.
        door.Comp.NextStateChange = GameTiming.CurTime + door.Comp.DoorStunTime;

        switch (door.Comp.State)
        {
            case DoorState.ClosingInProgress:
                SetState(door, DoorState.Open);

                break;
            case DoorState.OpeningInProgress:
                SetState(door, DoorState.Closed);

                break;
            case DoorState.Closed:
            case DoorState.AttemptingCloseBySelf:
            case DoorState.AttemptingCloseByPrying:
            case DoorState.Open:
            case DoorState.AttemptingOpenBySelf:
            case DoorState.AttemptingOpenByPrying:
            case DoorState.WeldedClosed:
            case DoorState.Denying:
            case DoorState.Emagging:
            default:
                return;
        }
    }

    /// <summary>
    ///     Get all entities that collide with this door by more than <see cref="IntersectPercentage"/> percent.
    /// </summary>
    public IEnumerable<EntityUid> GetColliding(EntityUid uid, PhysicsComponent? physics = null)
    {
        // Not touching this during the first pass.

        if (!Resolve(uid, ref physics))
            yield break;

        var xform = Transform(uid);
        // Getting the world bounds from the gridUid allows us to use the version of
        // GetCollidingEntities that returns Entity<PhysicsComponent>
        if (!TryComp<MapGridComponent>(xform.GridUid, out var mapGridComp))
            yield break;
        var tileRef = _mapSystem.GetTileRef(xform.GridUid.Value, mapGridComp, xform.Coordinates);

        _doorIntersecting.Clear();
        _entityLookup.GetLocalEntitiesIntersecting(xform.GridUid.Value,
            tileRef.GridIndices,
            _doorIntersecting,
            gridComp: mapGridComp,
            flags: (LookupFlags.All & ~LookupFlags.Sensors));

        // TODO SLOTH fix electro's code.
        // ReSharper disable once InconsistentNaming

        foreach (var otherPhysics in _doorIntersecting)
        {
            if (otherPhysics.Comp == physics)
                continue;

            if (!otherPhysics.Comp.CanCollide)
                continue;

            //TODO: Make only shutters ignore these objects upon colliding instead of all airlocks
            // Excludes Glasslayer for windows, GlassAirlockLayer for windoors, TableLayer for tables
            if (otherPhysics.Comp.CollisionLayer == (int)CollisionGroup.GlassLayer ||
                otherPhysics.Comp.CollisionLayer == (int)CollisionGroup.GlassAirlockLayer ||
                otherPhysics.Comp.CollisionLayer == (int)CollisionGroup.TableLayer)
                continue;

            //If the colliding entity is a slippable item ignore it by the airlock
            if (otherPhysics.Comp.CollisionLayer == (int)CollisionGroup.SlipLayer &&
                otherPhysics.Comp.CollisionMask == (int)CollisionGroup.ItemMask)
                continue;

            //For when doors need to close over conveyor belts
            if (otherPhysics.Comp.CollisionLayer == (int)CollisionGroup.ConveyorMask)
                continue;

            if ((physics.CollisionMask & otherPhysics.Comp.CollisionLayer) == 0 &&
                (otherPhysics.Comp.CollisionMask & physics.CollisionLayer) == 0)
                continue;

            yield return otherPhysics.Owner;
        }
    }

    private void PreventCollision(Entity<DoorComponent> door, ref PreventCollideEvent args)
    {
        if (!door.Comp.CurrentlyCrushing.Contains(args.OtherEntity))
            return;

        args.Cancelled = true;
    }

    /// <summary>
    /// Open a door if a player or door-bumper (PDA, ID-card) collide with the door. Sadly, bullets no longer generate
    /// "access denied" sounds as you fire at a door.
    /// </summary>
    private void HandleCollide(Entity<DoorComponent> door, ref StartCollideEvent args)
    {
        if (!door.Comp.BumpOpen)
            return;

        if (door.Comp.State is not (DoorState.Closed or DoorState.Denying))
            return;

        var otherUid = args.OtherEntity;

        if (Tags.HasTag(otherUid, DoorBumpTag))
            TryOpen(door, otherUid, quiet: door.Comp.State == DoorState.Denying, predicted: true);
    }

    #endregion

    #region Access

    /// <summary>
    /// Does the user have the permissions required to open this door?
    /// </summary>
    public bool HasAccess(Entity<DoorComponent> door, EntityUid? user = null, AccessReaderComponent? access = null)
    {
        // TODO network AccessComponent for predicting doors

        // if there is no "user" we skip the access checks. Access is also ignored in some game-modes.
        if (user == null || AccessType == AccessTypes.AllowAll)
            return true;

        // If the door is on emergency access we skip the checks.
        if (TryComp<AirlockComponent>(door, out var airlock) && airlock.EmergencyAccess)
            return true;

        // Anyone can click to open firelocks
        if (door.Comp.State == DoorState.Closed &&
            TryComp<FirelockComponent>(door, out _))
            return true;

        if (!Resolve(door, ref access, false))
            return true;

        var isExternal = access.AccessLists.Any(list => list.Contains("External"));

        return AccessType switch
        {
            // Some game modes modify access rules.
            AccessTypes.AllowAllIdExternal => !isExternal || _accessReaderSystem.IsAllowed(user.Value, door, access),
            AccessTypes.AllowAllNoExternal => !isExternal,
            _ => _accessReaderSystem.IsAllowed(user.Value, door, access)
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
    public void SetNextStateChange(Entity<DoorComponent> door, TimeSpan? delay)
    {
        // If the door is not currently just open or closed, it is busy doing something else (or welded shut). So in
        // that case we do nothing.
        if (door.Comp.State != DoorState.Open && door.Comp.State != DoorState.Closed)
            return;

        // Is this trying to prevent an update? (e.g., cancel an auto-close)
        if (delay == null || delay.Value <= TimeSpan.Zero)
        {
            door.Comp.NextStateChange = null;
            _activeDoors.Remove((door, door));

            return;
        }

        door.Comp.NextStateChange = GameTiming.CurTime + delay.Value;
        Dirty(door);

        _activeDoors.Add((door, door));
    }

    protected void CheckDoorBump(Entity<DoorComponent, PhysicsComponent> ent)
    {
        var (uid, door, physics) = ent;
        if (!door.BumpOpen)
            return;

        foreach (var other in PhysicsSystem.GetContactingEntities(uid, physics))
        {
            if (Tags.HasTag(other, DoorBumpTag) && TryOpen(ent, other, quiet: true))
                break;
        }
    }

    /// <summary>
    ///     Iterate over active doors and progress them to the next state if they need to be updated.
    /// </summary>
    public override void Update(float frameTime)
    {
        var time = GameTiming.CurTime;

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
                NextState(door, time);

            if (door.Comp.State != DoorState.Closed || !TryComp<PhysicsComponent>(door, out var doorBody))
                continue;

            // If something bumped into us during closing then start to re-open, otherwise, remove it from active.
            _activeDoors.Remove(door);
            CheckDoorBump((door, door, doorBody));
        }
    }

    /// <summary>
    ///     Makes a door proceed to the next state (if applicable).
    /// </summary>
    private void NextState(Entity<DoorComponent> door, TimeSpan time)
    {
        door.Comp.NextStateChange = null;

        if (door.Comp.CurrentlyCrushing.Count > 0)
            // This is a closed door that is crushing people and needs to auto-open. Note that we don't check "can open"
            // here. The door never actually finished closing and we don't want people to get stuck inside of doors.
            StartOpening(door, door);

        switch (door.Comp.State)
        {
            case DoorState.AttemptingOpenByPrying:
            case DoorState.AttemptingOpenBySelf:
                OnPartialOpen(door);

                return;
            case DoorState.OpeningInProgress:
                SetState(door, DoorState.Open);

                return;
            case DoorState.AttemptingCloseBySelf:
            case DoorState.AttemptingCloseByPrying:
                OnPartialClose(door);

                return;
            case DoorState.ClosingInProgress:
            case DoorState.Denying:
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
                    door.Comp.NextStateChange = time + TimeSpan.FromSeconds(1);
                }

                return;
            case DoorState.WeldedClosed:
                // A welded door? This should never have been active in the first place.
                Log.Error($"Welded door was in the list of active doors. Door: {ToPrettyString(door)}");

                return;
            case DoorState.Closed:
            default:
                return;
        }
    }

    #endregion
}
