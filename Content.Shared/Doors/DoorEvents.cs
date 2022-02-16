using Content.Shared.Doors.Components;
using Robust.Shared.GameObjects;

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
    /// Raised when the door is determining whether it is able to open.
    /// Cancel to stop the door from being opened.
    /// </summary>
    public sealed class BeforeDoorOpenedEvent : CancellableEntityEventArgs
    {
    }

    /// <summary>
    /// Raised when the door is determining whether it is able to close.
    /// Cancel to stop the door from being closed.
    /// </summary>
    /// <remarks>
    /// This event is raised both when the door is initially closed, and when it is just about to become "partially"
    /// closed (opaque & collidable). If canceled while partially closing, it will start opening again. Useful for
    /// things like airlock anti-crush safety features.
    /// </remarks>
    public sealed class BeforeDoorClosedEvent : CancellableEntityEventArgs
    {
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

    /// <summary>
    /// Raised to determine how long the door's pry time should be modified by.
    /// Multiply PryTimeModifier by the desired amount.
    /// </summary>
    public sealed class DoorGetPryTimeModifierEvent : EntityEventArgs
    {
        public float PryTimeModifier = 1.0f;
    }

    /// <summary>
    /// Raised when an attempt to pry open the door is made.
    /// Cancel to stop the door from being pried open.
    /// </summary>
    public sealed class BeforeDoorPryEvent : CancellableEntityEventArgs
    {
        public readonly EntityUid User;

        public BeforeDoorPryEvent(EntityUid user)
        {
            User = user;
        }
    }
}
