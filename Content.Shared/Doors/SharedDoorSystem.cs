using Content.Shared.Damage;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Shared.Doors;

public abstract class SharedDoorSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] protected readonly IGameTiming GameTiming = default!;

    /// <summary>
    ///     A body must have an intersection percentage larger than this in order to be considered as colliding with a
    ///     door. Used for safety close-blocking and crushing.
    /// </summary>
    /// <remarks>
    ///     The intersection percentage relies on WORLD AABBs. So if this is too small, and the grid is rotated 45
    ///     degrees, then an entity outside of the airlock may be crushed.
    /// </remarks>
    public const float IntersectPercentage = 0.2f;

    protected readonly HashSet<DoorComponent> ActiveDoors = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoorComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<DoorComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<DoorComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<DoorComponent, ComponentHandleState>(OnHandleState);

        SubscribeLocalEvent<DoorComponent, ActivateInWorldEvent>(OnActivate);

        SubscribeLocalEvent<DoorComponent, StartCollideEvent>(HandleCollide);
        SubscribeLocalEvent<DoorComponent, PreventCollideEvent>(PreventCollision);
    }

    private void OnStartup(EntityUid uid, DoorComponent door, ComponentStartup args)
    {
        // if the door state is not standard (i.e., door starts open), make sure collision & occlusion are properly set.
        if (door.StartOpen)
        {
            // disable occluder & physics
            OnPartialOpen(uid, door);

            // THEN set the correct state, inc disabling partial = true
            SetState(uid, DoorState.Open, door);

            // The airlock component may schedule an auto-close for this door during the SetState.
            // Give the door is supposed to start open, let's prevent any auto-closing that might occur.
            door.NextStateChange = null;
        }

        UpdateAppearance(uid, door);
    }

    private void OnShutdown(EntityUid uid, DoorComponent door, ComponentShutdown args)
    {
        if (ActiveDoors.Contains(door))
            ActiveDoors.Remove(door);
    }

    #region StateManagement
    private void OnGetState(EntityUid uid, DoorComponent door, ref ComponentGetState args)
    {
        args.State = new DoorComponentState(door);
    }

    private void OnHandleState(EntityUid uid, DoorComponent door, ref ComponentHandleState args)
    {
        if (args.Current is not DoorComponentState state)
            return;

        door.CurrentlyCrushing = state.CurrentlyCrushing;
        door.State = state.DoorState;
        door.NextStateChange = state.NextStateChange;
        door.Partial = state.Partial;
        door.Safety = state.Safety;

        if (state.NextStateChange == null)
            ActiveDoors.Remove(door);
        else
            ActiveDoors.Add(door);

        RaiseLocalEvent(uid, new DoorStateChangedEvent(door.State), false);
        UpdateAppearance(uid, door);
    }

    protected void SetState(EntityUid uid, DoorState state, DoorComponent? door = null)
    {
        if (!Resolve(uid, ref door))
            return;

        switch (state)
        {
            case DoorState.Opening:
                ActiveDoors.Add(door);
                door.NextStateChange = GameTiming.CurTime + door.OpenTimeOne;
                break;

            case DoorState.Closing:
                ActiveDoors.Add(door);
                door.NextStateChange = GameTiming.CurTime + door.CloseTimeOne;
                break;

            case DoorState.Denying:
                ActiveDoors.Add(door);
                door.NextStateChange = GameTiming.CurTime + door.DenyDuration;
                break;

            case DoorState.Open:
            case DoorState.Closed:
                door.Partial = false;
                break;
        }

        door.State = state;
        door.Dirty();
        RaiseLocalEvent(uid, new DoorStateChangedEvent(state), false);
        UpdateAppearance(uid, door);
    }

    public void SetSafety(EntityUid uid, bool safety, DoorComponent? door = null)
    {
        if (!Resolve(uid, ref door))
            return;

        door.Safety = safety;
        door.Dirty();
    }

    protected virtual void UpdateAppearance(EntityUid uid, DoorComponent? door = null)
    {
        if (!Resolve(uid, ref door))
            return;

        if (!TryComp(uid, out AppearanceComponent? appearance))
            return;

        appearance.SetData(DoorVisuals.State, door.State);
    }
    #endregion

    #region Interactions
    private void OnActivate(EntityUid uid, DoorComponent door, ActivateInWorldEvent args)
    {
        if (args.Handled || !door.ClickOpen)
            return;

        if (door.State == DoorState.Open)
        {
            TryClose(uid, door, args.User);
            args.Handled = true;
        }
        else if (door.State == DoorState.Closed)
        {
            TryOpen(uid, door, args.User);
            args.Handled = true;
        }
    }

    public void Deny(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool predicted = false)
    {
        if (!Resolve(uid, ref door))
            return;

        if (door.State != DoorState.Closed)
            return;

        // might not be able to deny without power or some other blocker.
        var ev = new BeforeDoorDeniedEvent();
        RaiseLocalEvent(uid, ev, false);
        if (ev.Cancelled)
            return;

        SetState(uid, DoorState.Denying, door);

        if (door.DenySound != null)
            PlaySound(uid, door.DenySound.GetSound(), AudioParams.Default.WithVolume(-3), user, predicted);
    }
    #endregion

    #region Opening
    public bool TryOpen(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool predicted = false)
    {
        if (!Resolve(uid, ref door))
            return false;

        if (!CanOpen(uid, door, user, false))
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

        var ev = new DoorOpenAttemptEvent();
        RaiseLocalEvent(uid, ev, false);
        if (ev.Cancelled)
            return false;

        if (!HasAccess(uid, user))
        {
            if (!quiet)
                Deny(uid, door);
            return false;
        }

        return true;
    }

    // Convenience function to make using Door system more intuitive for other systems.
    public virtual void StartOpening(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool predicted = false)
    {
        if (!Resolve(uid, ref door))
            return;

        SetState(uid, DoorState.Opening, door);

        if (door.OpenSound != null)
            PlaySound(uid, door.OpenSound.GetSound(), AudioParams.Default.WithVolume(-5), user, predicted);

        // I'm not sure what the intent here is/was? It plays a sound if the user is opening a door with a hands
        // component, but no actual hands!? What!? Is this the sound of them head-butting the door to get it to open??
        // I'm 99% sure something is wrong here, but I kind of want to keep it this way.

        if (user != null && TryComp(user.Value, out SharedHandsComponent? hands) && hands.Hands.Count == 0)
            PlaySound(uid, door.TryOpenDoorSound.GetSound(), AudioParams.Default.WithVolume(-2), user, predicted);
    }

    /// <summary>
    /// Called when the door is partially opened.
    /// </summary>
    public virtual void OnPartialOpen(EntityUid uid, DoorComponent? door = null, PhysicsComponent? physics = null)
    {
        if (!Resolve(uid, ref door, ref physics))
            return;

        // we can't be crushing anyone anymore, since we're opening
        door.CurrentlyCrushing.Clear();
        physics.CanCollide = false;
        door.Partial = true;
        door.NextStateChange = GameTiming.CurTime + door.CloseTimeTwo;
        ActiveDoors.Add(door);
        door.Dirty();

        if (door.Occludes && TryComp(uid, out OccluderComponent? occluder))
            occluder.Enabled = false;
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

    public bool CanClose(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool quiet = true)
    {
        if (!Resolve(uid, ref door))
            return false;

        var ev = new DoorCloseAttemptEvent();
        RaiseLocalEvent(uid, ev, false);
        if (ev.Cancelled)
            return false;

        if (door.Safety && GetColliding(uid).Any())
            return false;

        return HasAccess(uid, user);
    }

    // Convenience function to make using Door system more intuitive for other systems.
    public virtual void StartClosing(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool predicted = false)
    {
        if (!Resolve(uid, ref door))
            return;

        SetState(uid, DoorState.Closing, door);

        if (door.CloseSound != null)
            PlaySound(uid, door.CloseSound.GetSound(), AudioParams.Default.WithVolume(-5), user, predicted);
    }

    /// <summary>
    /// Called when the door is partially closed. This is when the door becomes "solid". If this process fails (e.g., a
    /// mob entered the door as it was closing), then this returns false. Otheriwse, returns true;
    /// </summary>
    public virtual bool OnPartialClose(EntityUid uid, DoorComponent? door = null, PhysicsComponent? physics = null)
    {
        if (!Resolve(uid, ref door, ref physics))
            return false;

        door.Partial = true;

        // Make sure no entity waled into the airlock when it started closing.
        if (door.Safety && GetColliding(uid).Any())
        {
            ActiveDoors.Add(door);
            door.NextStateChange = GameTiming.CurTime + door.OpenTimeTwo;
            door.State = DoorState.Opening;
            door.Dirty();
            UpdateAppearance(uid, door);
            return false;
        }

        physics.CanCollide = true;
        door.NextStateChange = GameTiming.CurTime + door.CloseTimeTwo;
        ActiveDoors.Add(door);
        door.Dirty();

        if (door.Occludes && TryComp(uid, out OccluderComponent? occluder))
            occluder.Enabled = true;

        // Crush any entities. Note that we don't check airlock safety here. This should have been checked before
        // the door closed.
        Crush(uid, door, physics);
        return true;
    }
    #endregion

    #region Collisions
    /// <summary>
    /// Crushes everyone colliding with us by more than 10%.
    /// </summary>
    /// <returns>True if we crushed somebody, false if we did not.</returns>
    public void Crush(EntityUid uid, DoorComponent? door = null, PhysicsComponent? physics = null)
    {
        if (!Resolve(uid, ref door))
            return;

        // is this door capable of crushing? NOT the same as an airlock safety check. The door will still close.
        if (!door.CanCrush) 
            return;

        // Crush
        var stunTime = door.DoorStunTime + door.OpenTimeOne;
        foreach (var entity in GetColliding(uid, physics))
        {
            door.CurrentlyCrushing.Add(entity);
            if (door.CrushDamage != null)
                _damageableSystem.TryChangeDamage(entity, door.CrushDamage);

            if (TryComp(entity, out StatusEffectsComponent? status))
                _stunSystem.TryParalyze(entity, stunTime, true, status);
        }

        if (door.CurrentlyCrushing.Count == 0)
            return;

        // queue the door to open so that the player is no longer stunned once it has FINISHED opening.
        door.NextStateChange = GameTiming.CurTime + door.DoorStunTime;
        door.Partial = false;
    }

    /// <summary>
    ///     Get all entities that collide with this door by more than <see cref="IntersectPercentage"/> percent.
    public IEnumerable<EntityUid> GetColliding(EntityUid uid, PhysicsComponent? physics = null)
    {
        if (!Resolve(uid, ref physics))
            yield break;

        var doorAABB = physics.GetWorldAABB();

        foreach (var body in _physicsSystem.GetCollidingEntities(Transform(uid).MapID, doorAABB))
        {
            // static bodies (e.g., furniture) shouldn't stop airlocks/windoors from closing.
            if (body.BodyType == BodyType.Static)
                continue;

            if (body.GetWorldAABB().IntersectPercentage(doorAABB) < IntersectPercentage)
                continue;

            yield return body.Owner;
        }
    }

    private void PreventCollision(EntityUid uid, DoorComponent component, PreventCollideEvent args)
    {
        if (component.CurrentlyCrushing.Contains(args.BodyB.Owner))
        {
            args.Cancel();
        }
    }

    protected virtual void HandleCollide(EntityUid uid, DoorComponent door, StartCollideEvent args)
    {
        // TODO ACCESS READER move access reader to shared and predict door opening/closing
        // Then this can be moved to the shared system without mispredicting.
    }
    #endregion

    #region Access
    public virtual bool HasAccess(EntityUid uid, EntityUid? user = null)
    {
        // TODO move access reader to shared for predicting door opening
        return true;
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
    ///     Schedule for a door update it's state after some time. Useful for third-party systems, like the AirlockSystem power-dependency.
    /// </summary>
    public void SetNextStateChange(EntityUid uid, TimeSpan? delay, DoorComponent? door = null)
    {
        if (!Resolve(uid, ref door))
            return;

        // if the door is currently in the process of opening or closing we will always let that finish and ignore this request.
        if (door.State == DoorState.Opening || door.State == DoorState.Closing)
            return;

        // is this trying to prevent an update? (e.g., cancel an auto-close)
        if (delay == null)
        {
            door.NextStateChange = null;
            ActiveDoors.Remove(door);
            return;
        }

        // if an update is already set to happen before this delay, don't do anything
        var time = GameTiming.CurTime + delay.Value;
        if (door.NextStateChange != null && door.NextStateChange.Value < time)
            return;

        door.NextStateChange = time;
        ActiveDoors.Add(door);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        HashSet<DoorComponent> toRemove = new();

        // cache CurTime (getter function)
        var time = GameTiming.CurTime;

        foreach (var door in ActiveDoors)
        {
            if (door.Deleted || door.NextStateChange == null)
            {
                toRemove.Add(door);
                continue;
            }

            if (door.Paused)
                continue;

            if (door.NextStateChange.Value < time)
            {
                // Progress the door to the next state. This may sometimes call ActiveDoors.Add(...), but only for existing
                // doors, and so it should never result in an enumerator modification error.
                NextState(door, time);
            }
        }

        ActiveDoors.ExceptWith(toRemove);
    }

    /// <summary>
    ///     Updates an active door. Returns true if the door has finished updating, false otherwise.
    /// </summary>
    private void NextState(DoorComponent door, TimeSpan time)
    {
        door.NextStateChange = null;

        if (door.CurrentlyCrushing.Count > 0)
            // This is a closed door that is crushing people and needs to auto-open. Note that we don't check "can open"
            // here. The door never actually finished closing and we don't want people to get stuck in walls.
            StartOpening(door.Owner, door, predicted: true);

        switch (door.State)
        {
            
            case DoorState.Opening:
                // Either fully or partially open this door.
                if (door.Partial)
                    SetState(door.Owner, DoorState.Open, door);
                else
                    OnPartialOpen(door.Owner, door);

                break;

            case DoorState.Closing:
                // Either fully or partially close this door.
                if (door.Partial)
                    SetState(door.Owner, DoorState.Closed, door);
                else
                    OnPartialClose(door.Owner, door);

                break;

            case DoorState.Denying:
                // Finish denying entry and return to the closed state.
                SetState(door.Owner, DoorState.Closed, door);
                break;

            case DoorState.Open:
                // This door is open, and queued for an auto-close.
                if (!TryClose(door.Owner, door, predicted: true))
                {
                    // The door failed to close (blocked?). Try again in one second.
                    door.NextStateChange = time + TimeSpan.FromSeconds(1);
                }
                break;

            default:
                // A welded door? This should never happen.
                door.NextStateChange = null;
                break;
        }
    }
    #endregion

    protected abstract void PlaySound(EntityUid uid, string sound, AudioParams audioParams, EntityUid? predictingPlayer, bool predicted);
}
