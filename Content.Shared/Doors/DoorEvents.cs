using Content.Shared.Doors.Components;

namespace Content.Shared.Doors
{
    /// <summary>
    /// Raised when the door's State variable is changed to a new variable that it was not equal to before.
    /// </summary>
    public sealed class DoorStateChangedEvent : EntityEventArgs
    {
        public readonly DoorState State;

        public DoorStateChangedEvent(DoorState state)
        {
            State = state;
        }
    }

    /// <summary>
    /// Raised when the door's bolt status was changed.
    /// </summary>
    public sealed class DoorBoltsChangedEvent : EntityEventArgs
    {
        public readonly bool BoltsDown;

        public DoorBoltsChangedEvent(bool boltsDown)
        {
            BoltsDown = boltsDown;
        }
    }

    /// <summary>
    /// Raised when the door is determining whether it is able to open.
    /// Cancel to stop the door from being opened.
    /// </summary>
    public sealed class BeforeDoorOpenedEvent : CancellableEntityEventArgs
    {
        public EntityUid? User = null;
    }

    /// <summary>
    /// Raised when the door is determining whether it is able to close. If the event is canceled, the door will not
    /// close. Additionally this event also has a bool that determines whether or not the door should perform a
    /// safety/collision check before closing. This check has to be proactively disabled by things like hacked airlocks.
    /// </summary>
    /// <remarks>
    /// This event is raised both when the door is initially closed, and when it is just about to become "partially"
    /// closed (opaque & collidable). If canceled while partially closing, it will start opening again. Useful in case
    /// an entity entered the door just as it was about to become "solid".
    /// </remarks>
    public sealed class BeforeDoorClosedEvent : CancellableEntityEventArgs
    {
        public bool PerformCollisionCheck;

        public BeforeDoorClosedEvent(bool performCollisionCheck)
        {
            PerformCollisionCheck = performCollisionCheck;
        }
    }

    /// <summary>
    /// Called when the door is determining whether it is able to deny.
    /// Cancel to stop the door from being able to deny.
    /// </summary>
    public sealed class BeforeDoorDeniedEvent : CancellableEntityEventArgs
    {
    }

    /// <summary>
    /// Raised to determine whether the door should automatically close.
    /// Cancel to stop it from automatically closing.
    /// </summary>
    /// <remarks>
    /// This is called when a door decides whether it SHOULD auto close, not when it actually closes.
    /// </remarks>
    public sealed class BeforeDoorAutoCloseEvent : CancellableEntityEventArgs
    {
    }
}
