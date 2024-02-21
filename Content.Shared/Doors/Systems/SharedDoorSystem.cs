using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Damage;
using Content.Shared.Doors.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Prying.Components;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Doors.Systems;

public abstract class SharedDoorSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] protected readonly SharedPhysicsSystem PhysicsSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] protected readonly TagSystem Tags = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] protected readonly SharedAppearanceSystem AppearanceSystem = default!;
    [Dependency] private readonly OccluderSystem _occluder = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;

    /// <summary>
    ///     A body must have an intersection percentage larger than this in order to be considered as colliding with a
    ///     door. Used for safety close-blocking and crushing.
    /// </summary>
    /// <remarks>
    ///     The intersection percentage relies on WORLD AABBs. So if this is too small, and the grid is rotated 45
    ///     degrees, then an entity outside of the airlock may be crushed.
    /// </remarks>
    public const float IntersectPercentage = 0.2f;

    /// <summary>
    ///     A set of doors that are currently opening, closing, or just queued to open/close after some delay.
    /// </summary>
    private readonly HashSet<Entity<DoorComponent>> _activeDoors = new();

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
        SubscribeLocalEvent<DoorComponent, GetPryTimeModifierEvent>(OnPryTimeModifier);

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

    #region StateManagement
    private void OnHandleState(Entity<DoorComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var door = ent.Comp;
        if (door.NextStateChange == null)
            _activeDoors.Remove(ent);
        else
            _activeDoors.Add(ent);

        RaiseLocalEvent(ent, new DoorStateChangedEvent(door.State));
        AppearanceSystem.SetData(ent, DoorVisuals.State, door.State);
    }

    protected void SetState(EntityUid uid, DoorState state, DoorComponent? door = null)
    {
        if (!Resolve(uid, ref door))
            return;

        // If no change, return to avoid firing a new DoorStateChangedEvent.
        if (state == door.State)
            return;

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
    }

    #endregion

    #region Interactions
    protected virtual void OnActivate(EntityUid uid, DoorComponent door, ActivateInWorldEvent args)
    {
        // avoid client-mispredicts, as the server will definitely handle this event
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

        SetState(uid, DoorState.Denying, door);

        if (door.DenySound != null)
            PlaySound(uid, door.DenySound, AudioParams.Default.WithVolume(-3), user, predicted);
    }


    public bool TryToggleDoor(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool predicted = false)
    {
        if (!Resolve(uid, ref door))
            return false;

        if (door.State is DoorState.Closed or DoorState.Denying)
        {
            return TryOpen(uid, door, user, predicted, quiet: door.State == DoorState.Denying);
        }
        else if (door.State == DoorState.Open)
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

        var ev = new BeforeDoorOpenedEvent() { User = user };
        RaiseLocalEvent(uid, ev);
        if (ev.Cancelled)
            return false;

        if (!HasAccess(uid, user, door))
        {
            if (!quiet)
                Deny(uid, door);
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
    public virtual void StartOpening(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool predicted = false)
    {
        if (!Resolve(uid, ref door))
            return;

        SetState(uid, DoorState.Opening, door);

        if (door.OpenSound != null)
            PlaySound(uid, door.OpenSound, AudioParams.Default.WithVolume(-5), user, predicted);

        // I'm not sure what the intent here is/was? It plays a sound if the user is opening a door with a hands
        // component, but no actual hands!? What!? Is this the sound of them head-butting the door to get it to open??
        // I'm 99% sure something is wrong here, but I kind of want to keep it this way.

        if (user != null && TryComp(user.Value, out HandsComponent? hands) && hands.Hands.Count == 0)
            PlaySound(uid, door.TryOpenDoorSound, AudioParams.Default.WithVolume(-2), user, predicted);
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
        door.NextStateChange = GameTiming.CurTime + door.CloseTimeTwo;
        _activeDoors.Add((uid, door));
        Dirty(uid, door);

    }
    #endregion

    #region Closing
    public bool TryClose(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool predicted = false)
    {
        if (!Resolve(uid, ref door))
            return false;

        if (!CanClose(uid, door, user, false))
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
    /// <param name="predicted">Whether the interaction would have been
    /// predicted. See comments in the PlaySound method on the Server system for details</param>
    public bool CanClose(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool quiet = true)
    {
        if (!Resolve(uid, ref door))
            return false;

        // since both closing/closed and welded are door states, we need to prevent 'closing'
        // a welded door or else there will be weird state bugs
        if (door.State is DoorState.Welded or DoorState.Closed)
            return false;

        var ev = new BeforeDoorClosedEvent(door.PerformCollisionCheck);
        RaiseLocalEvent(uid, ev, false);
        if (ev.Cancelled)
            return false;

        if (!HasAccess(uid, user, door))
            return false;

        return !ev.PerformCollisionCheck || !GetColliding(uid).Any();
    }

    public virtual void StartClosing(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool predicted = false)
    {
        if (!Resolve(uid, ref door))
            return;

        SetState(uid, DoorState.Closing, door);

        if (door.CloseSound != null)
            PlaySound(uid, door.CloseSound, AudioParams.Default.WithVolume(-5), user, predicted);
    }

    /// <summary>
    /// Called when the door is partially closed. This is when the door becomes "solid". If this process fails (e.g., a
    /// mob entered the door as it was closing), then this returns false. Otherwise, returns true;
    /// </summary>
    public bool OnPartialClose(EntityUid uid, DoorComponent? door = null, PhysicsComponent? physics = null)
    {
        if (!Resolve(uid, ref door, ref physics))
            return false;

        door.Partial = true;
        Dirty(uid, door);

        // Make sure no entity waled into the airlock when it started closing.
        if (!CanClose(uid, door))
        {
            door.NextStateChange = GameTiming.CurTime + door.OpenTimeTwo;
            door.State = DoorState.Opening;
            AppearanceSystem.SetData(uid, DoorVisuals.State, DoorState.Opening);
            return false;
        }

        SetCollidable(uid, true, door, physics);
        door.NextStateChange = GameTiming.CurTime + door.CloseTimeTwo;
        _activeDoors.Add((uid, door));

        // Crush any entities. Note that we don't check airlock safety here. This should have been checked before
        // the door closed.
        Crush(uid, door, physics);
        return true;
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
    /// Crushes everyone colliding with us by more than <see cref="IntersectPercentage"/>%.
    /// </summary>
    public void Crush(EntityUid uid, DoorComponent? door = null, PhysicsComponent? physics = null)
    {
        if (!Resolve(uid, ref door))
            return;

        if (!door.CanCrush)
            return;

        // Find entities and apply curshing effects
        var stunTime = door.DoorStunTime + door.OpenTimeOne;
        foreach (var entity in GetColliding(uid, physics))
        {
            door.CurrentlyCrushing.Add(entity);
            if (door.CrushDamage != null)
                _damageableSystem.TryChangeDamage(entity, door.CrushDamage, origin: uid);

            _stunSystem.TryParalyze(entity, stunTime, true);
        }

        if (door.CurrentlyCrushing.Count == 0)
            return;

        // queue the door to open so that the player is no longer stunned once it has FINISHED opening.
        door.NextStateChange = GameTiming.CurTime + door.DoorStunTime;
        door.Partial = false;
    }

    /// <summary>
    ///     Get all entities that collide with this door by more than <see cref="IntersectPercentage"/> percent.\
    /// </summary>
    public IEnumerable<EntityUid> GetColliding(EntityUid uid, PhysicsComponent? physics = null)
    {
        if (!Resolve(uid, ref physics))
            yield break;

        // TODO SLOTH fix electro's code.
        // ReSharper disable once InconsistentNaming
        var doorAABB = _entityLookup.GetWorldAABB(uid);

        foreach (var otherPhysics in PhysicsSystem.GetCollidingEntities(Transform(uid).MapID, doorAABB))
        {
            if (otherPhysics == physics)
                continue;

            //TODO: Make only shutters ignore these objects upon colliding instead of all airlocks
            // Excludes Glasslayer for windows, GlassAirlockLayer for windoors, TableLayer for tables
            if (!otherPhysics.CanCollide || otherPhysics.CollisionLayer == (int)CollisionGroup.GlassLayer || otherPhysics.CollisionLayer == (int)CollisionGroup.GlassAirlockLayer || otherPhysics.CollisionLayer == (int)CollisionGroup.TableLayer)
                continue;

            //If the colliding entity is a slippable item ignore it by the airlock
            if (otherPhysics.CollisionLayer == (int)CollisionGroup.SlipLayer && otherPhysics.CollisionMask == (int)CollisionGroup.ItemMask)
                continue;

            //For when doors need to close over conveyor belts
            if (otherPhysics.CollisionLayer == (int) CollisionGroup.ConveyorMask)
                continue;

            if ((physics.CollisionMask & otherPhysics.CollisionLayer) == 0 && (otherPhysics.CollisionMask & physics.CollisionLayer) == 0)
                continue;

            if (_entityLookup.GetWorldAABB(otherPhysics.Owner).IntersectPercentage(doorAABB) < IntersectPercentage)
                continue;

            yield return otherPhysics.Owner;
        }
    }

    private void PreventCollision(EntityUid uid, DoorComponent component, ref PreventCollideEvent args)
    {
        if (component.CurrentlyCrushing.Contains(args.OtherEntity))
        {
            args.Cancelled = true;
        }
    }

    protected virtual void HandleCollide(EntityUid uid, DoorComponent door, ref StartCollideEvent args)
    {
        // TODO ACCESS READER move access reader to shared and predict door opening/closing
        // Then this can be moved to the shared system without mispredicting.
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
            door.NextStateChange = null;
            _activeDoors.Remove((uid, door));
            return;
        }

        door.NextStateChange = GameTiming.CurTime + delay.Value;
        _activeDoors.Add((uid, door));
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

    protected virtual void CheckDoorBump(Entity<DoorComponent, PhysicsComponent> ent) { }

    /// <summary>
    ///     Makes a door proceed to the next state (if applicable).
    /// </summary>
    private void NextState(Entity<DoorComponent> ent, TimeSpan time)
    {
        var door = ent.Comp;
        door.NextStateChange = null;

        if (door.CurrentlyCrushing.Count > 0)
            // This is a closed door that is crushing people and needs to auto-open. Note that we don't check "can open"
            // here. The door never actually finished closing and we don't want people to get stuck inside of doors.
            StartOpening(ent, door, predicted: true);

        switch (door.State)
        {
            case DoorState.Opening:
                // Either fully or partially open this door.
                if (door.Partial)
                    SetState(ent, DoorState.Open, door);
                else
                    OnPartialOpen(ent, door);

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
                if (!TryClose(ent, door, predicted: true))
                {
                    // The door failed to close (blocked?). Try again in one second.
                    door.NextStateChange = time + TimeSpan.FromSeconds(1);
                }
                break;

            case DoorState.Welded:
                // A welded door? This should never have been active in the first place.
                Log.Error($"Welded door was in the list of active doors. Door: {ToPrettyString(ent)}");
                break;
        }
    }
    #endregion

    protected abstract void PlaySound(EntityUid uid, SoundSpecifier soundSpecifier, AudioParams audioParams, EntityUid? predictingPlayer, bool predicted);
}
