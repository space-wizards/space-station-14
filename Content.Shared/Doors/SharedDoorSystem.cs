using Content.Shared.Damage;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using static Content.Shared.Doors.DoorComponent;

namespace Content.Shared.Doors;

public abstract class SharedDoorSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] protected readonly IGameTiming GameTiming = default!;

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
        if (!door.StartOpen)
            return;

        OnPartialOpen(uid, door);
        SetState(uid, DoorState.Open, door); // will also update appearance
    }

    private void OnShutdown(EntityUid uid, DoorComponent door, ComponentShutdown args)
    {
        if (ActiveDoors.Contains(door))
            ActiveDoors.Remove(door);
    }

    #region StateManagement
    private void OnGetState(EntityUid uid, DoorComponent door, ref ComponentGetState args)
    {
        args.State = new DoorComponentState(door.State, door.CurrentlyCrushing, door.TimeUntilStateChange);
    }

    private void OnHandleState(EntityUid uid, DoorComponent door, ref ComponentHandleState args)
    {
        if (args.Current is not DoorComponentState state)
            return;

        door.CurrentlyCrushing = state.CurrentlyCrushing;
        door.State = state.DoorState;
        door.TimeUntilStateChange = state.TimeUntilStateChange;

        if (door.TimeUntilStateChange > 0)
            ActiveDoors.Add(door);
        else
            ActiveDoors.Remove(door);

        RaiseLocalEvent(uid, new DoorStateChangedEvent(door.State), false);
        UpdateAppearance(uid, door);

        door.Dirty();
    }

    protected void SetState(EntityUid uid, DoorState state, DoorComponent? door = null)
    {
        if (!Resolve(uid, ref door))
            return;

        switch (state)
        {
            case DoorState.Opening:

                ActiveDoors.Add(door);
                door.TimeUntilStateChange = door.OpenTimeOne + door.OpenTimeTwo;
                break;

            case DoorState.Closing:
    
                ActiveDoors.Add(door);
                door.TimeUntilStateChange = door.CloseTimeOne + door.CloseTimeTwo;
                break;

            case DoorState.Denying:

                ActiveDoors.Add(door);
                door.TimeUntilStateChange = door.DenyDuration;
                break;
        }

        door.State = state;
        door.Dirty();
        RaiseLocalEvent(uid, new DoorStateChangedEvent(state), false);
        UpdateAppearance(uid, door);
    }

    protected virtual void UpdateAppearance(EntityUid uid, DoorComponent? door = null)
    {
        if (!Resolve(uid, ref door))
            return;

        if (!TryComp(uid, out AppearanceComponent? appearance))
            return;

        // TODO DOORS replace DoorVisualState with just DoorState?
        // Would have to separate out the "Denied" state.
        // but that should really be an airlock feature anyway, not a generic door state.

        appearance.SetData(DoorVisuals.State, door.State);
    }
    #endregion

    #region Interactions
    private void OnActivate(EntityUid uid, DoorComponent door, ActivateInWorldEvent args)
    {
        if (args.Handled || !door.ClickOpen)
            return;

        if (door.State == DoorState.Open)
            TryClose(uid, door, args.User);
        else if (door.State == DoorState.Closed)
            TryOpen(uid, door, args.User);
    }

    public void Deny(EntityUid uid, DoorComponent? door = null)
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
        door.TimeUntilStateChange = door.DenyDuration;
    }
    #endregion

    #region Opening
    public bool TryOpen(EntityUid uid, DoorComponent? door = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref door))
            return false;

        if (!CanOpen(uid, door, user, false))
            return false;

        StartOpening(uid, door);

        // I'm not sure what the intent here is/was?
        // Play a sound if the user is opening a door with a hands component, but no actual hands!? what?
         
        if (user != null && TryComp(user.Value, out SharedHandsComponent? hands) && hands.Hands.Count == 0)
        {
            SoundSystem.Play(Filter.Pvs(uid), door.TryOpenDoorSound.GetSound(), uid,
                AudioParams.Default.WithVolume(-2));
        }

        return true;
    }

    public bool CanOpen(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool quiet = true)
    {
        if (!Resolve(uid, ref door))
            return false;

        if (door.State == DoorState.Welded)
            return false;

        if (!HasAccess(uid, user))
        {
            if (!quiet)
                Deny(uid, door);
            return false;
        }

        var ev = new DoorOpenAttemptEvent();
        RaiseLocalEvent(uid, ev, false);
        return !ev.Cancelled;
    }

    // Convenience function to make using Door system more intuitive for other systems.
    public void StartOpening(EntityUid uid, DoorComponent? door = null)
    {
        if (!Resolve(uid, ref door))
            return;

        SetState(uid, DoorState.Opening, door);

        if (door.OpenSound != null && GameTiming.IsFirstTimePredicted)
        {
            SoundSystem.Play(Filter.Pvs(uid), door.OpenSound.GetSound(), uid,
                AudioParams.Default.WithVolume(-5));
        }
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

        if (door.Occludes && TryComp(uid, out OccluderComponent? occluder))
            occluder.Enabled = false;
    }
    #endregion

    #region Closing
    public bool TryClose(EntityUid uid, DoorComponent? door = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref door))
            return false;

        if (!CanClose(uid, door, user, false))
            return false;

        StartClosing(uid, door);
        return true;
    }

    public bool CanClose(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool quiet = true)
    {
        if (!Resolve(uid, ref door))
            return false;

        if (!HasAccess(uid, user))
            return false;

        var ev = new DoorCloseAttemptEvent();
        RaiseLocalEvent(uid, ev, false);

        return !ev.Cancelled;
    }

    // Convenience function to make using Door system more intuitive for other systems.
    public void StartClosing(EntityUid uid, DoorComponent? door = null)
    {
        if (!Resolve(uid, ref door))
            return;

        SetState(uid, DoorState.Closing, door);

        if (door.CloseSound != null && GameTiming.IsFirstTimePredicted)
        {
            SoundSystem.Play(Filter.Pvs(uid), door.CloseSound.GetSound(), uid,
                AudioParams.Default.WithVolume(-5));
        }
    }

    /// <summary>
    /// Called when the door is partially closed. This is when the door becomes "solid". If this process crushes mobs,
    /// returns true;
    /// </summary>
    public virtual void OnPartialClose(EntityUid uid, DoorComponent? door = null, PhysicsComponent? physics = null)
    {
        // door argument only used by server override.
        if (!Resolve(uid, ref door, ref physics))
            return;

        physics.CanCollide = true;

        if (door.Occludes && TryComp(uid, out OccluderComponent? occluder))
            occluder.Enabled = true;

        // Crush any entities. Note that we don't check airlock safety here. This should have been checked before
        // the door closed.
        Crush(uid, door, physics);
    }
    #endregion

    #region Collisions
    /// <summary>
    /// Crushes everyone colliding with us by more than 10%.
    /// </summary>
    /// <returns>True if we crushed somebody, false if we did not.</returns>
    public void Crush(EntityUid uid, DoorComponent? door = null, PhysicsComponent? physics = null)
    {
        if (!Resolve(uid, ref door, ref physics, false))
            return;

        // is this door capable of crushing? NOT the same as an airlock safety check. The door will still close.
        if (!door.CanCrush) 
            return;

        var collidingentities = _physicsSystem.GetCollidingEntities(physics, Vector2.Zero, false);

        if (!collidingentities.Any())
            return;

        var doorAABB = physics.GetWorldAABB();

        // Crush
        var stunTime = TimeSpan.FromSeconds(door.DoorStunTime + door.OpenTimeOne);
        foreach (var e in collidingentities)
        {
            var percentage = e.GetWorldAABB().IntersectPercentage(doorAABB);

            if (percentage < 0.1f)
                continue;

            door.CurrentlyCrushing.Add(e.Owner);
            if (door.CrushDamage != null)
                _damageableSystem.TryChangeDamage(e.Owner, door.CrushDamage);

            _stunSystem.TryParalyze(e.Owner, stunTime, true);
        }

        if (door.CurrentlyCrushing.Count > 0)
            door.TimeUntilStateChange = door.DoorStunTime + door.OpenTimeOne;
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
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        HashSet<DoorComponent> toRemove = new();

        foreach (var door in ActiveDoors)
        {
            if (door.Paused)
                continue;

            if (door.Deleted)
                toRemove.Add(door);
            else if (ProcessActiveDoor(frameTime, door))
            {
                toRemove.Add(door);
                door.TimeUntilStateChange = 0;
                door.Dirty();
            }
        }

        ActiveDoors.ExceptWith(toRemove);
    }

    /// <summary>
    ///     Updates an active door. Returns true if the door has finished updating, false otherwise.
    /// </summary>
    protected bool ProcessActiveDoor(float frameTime, DoorComponent door)
    {
        door.TimeUntilStateChange -= frameTime;
     
        if (door.CurrentlyCrushing.Count > 0)
        {
            if (door.TimeUntilStateChange < 0)
                StartOpening(door.Owner, door);
            return false;
        }

        // Some states (opening & closing) have intermediate steps. So we don't just check if TimeUntilStateChange > 0 and return
        // immediately here.

        switch (door.State)
        {
            case DoorState.Opening:

                if (door.TimeUntilStateChange < 0)
                {
                    SetState(door.Owner, DoorState.Open, door);
                    break;
                }

                if (door.TimeUntilStateChange < door.OpenTimeTwo)
                    OnPartialOpen(door.Owner, door);

                break;

            case DoorState.Closing:

                if (door.TimeUntilStateChange < 0)
                {
                    SetState(door.Owner, DoorState.Closed, door);
                    break;
                }

                if (door.TimeUntilStateChange < door.CloseTimeTwo)
                    OnPartialClose(door.Owner, door);

                break;

            case DoorState.Denying:

                if (door.TimeUntilStateChange < 0)
                {
                    SetState(door.Owner, DoorState.Closed, door);
                    return true;
                }

                break;

            case DoorState.Open:

                if (door.TimeUntilStateChange > 0)
                    break;

                if (TryClose(door.Owner, door))
                    break;

                // Try again in one second.
                door.TimeUntilStateChange += 1;
                break;

            default:
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Schedule an open door to be closed after some delay.
    /// </summary>
    public void QueueAutoClose(EntityUid uid, float delay, DoorComponent? door = null)
    {
        if (!Resolve(uid, ref door))
            return;

        if (door.State != DoorState.Open)
            return;

        door.Dirty();

        if (delay <= 0)
        {
            ActiveDoors.Remove(door);
            return;
        }

        ActiveDoors.Add(door);
        door.TimeUntilStateChange = delay;
    }
    #endregion
}
