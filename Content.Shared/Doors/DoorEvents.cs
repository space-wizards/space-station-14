using Content.Shared.Interaction;
using Robust.Shared.GameObjects;

namespace Content.Shared.Doors
{
    /// <summary>
    /// Raised when the door's State variable is changed to a new variable that it was not equal to before.
    /// </summary>
    public class DoorStateChangedEvent : EntityEventArgs
    {
        public SharedDoorComponent.DoorState State;

        public DoorStateChangedEvent(SharedDoorComponent.DoorState state)
        {
            State = state;
        }
    }

    /// <summary>
    /// Raised when the door is determining whether it is able to open.
    /// Cancel to stop the door from being opened.
    /// </summary>
    public class BeforeDoorOpenedEvent : CancellableEntityEventArgs
    {
    }

    /// <summary>
    /// Raised when the door is determining whether it is able to close.
    /// Cancel to stop the door from being closed.
    /// </summary>
    public class BeforeDoorClosedEvent : CancellableEntityEventArgs
    {
    }

    /// <summary>
    /// Called when the door is determining whether it is able to deny.
    /// Cancel to stop the door from being able to deny.
    /// </summary>
    public class BeforeDoorDeniedEvent : CancellableEntityEventArgs
    {
    }

    /// <summary>
    /// Raised to determine whether the door's safety is on.
    /// Modify Safety to set the door's safety.
    /// </summary>
    public class DoorSafetyEnabledEvent : HandledEntityEventArgs
    {
        public bool Safety = false;
    }

    /// <summary>
    /// Raised to determine whether the door should automatically close.
    /// Cancel to stop it from automatically closing.
    /// </summary>
    public class BeforeDoorAutoCloseEvent : CancellableEntityEventArgs
    {
    }

    /// <summary>
    /// Raised to determine how long the door's pry time should be modified by.
    /// Multiply PryTimeModifier by the desired amount.
    /// </summary>
    public class DoorGetPryTimeModifierEvent : EntityEventArgs
    {
        public float PryTimeModifier = 1.0f;
    }

    /// <summary>
    /// Raised to determine how long the door's close time should be modified by.
    /// Multiply CloseTimeModifier by the desired amount.
    /// </summary>
    public class DoorGetCloseTimeModifierEvent : EntityEventArgs
    {
        public float CloseTimeModifier = 1.0f;
    }

    /// <summary>
    /// Raised to determine whether clicking the door should open/close it.
    /// </summary>
    public class DoorClickShouldActivateEvent : HandledEntityEventArgs
    {
        public ActivateEventArgs Args;

        public DoorClickShouldActivateEvent(ActivateEventArgs args)
        {
            Args = args;
        }
    }

    /// <summary>
    /// Raised when an attempt to pry open the door is made.
    /// Cancel to stop the door from being pried open.
    /// </summary>
    public class BeforeDoorPryEvent : CancellableEntityEventArgs
    {
        public InteractUsingEventArgs Args;

        public BeforeDoorPryEvent(InteractUsingEventArgs args)
        {
            Args = args;
        }
    }

    /// <summary>
    /// Raised when a door is successfully pried open.
    /// </summary>
    public class OnDoorPryEvent : EntityEventArgs
    {
        public InteractUsingEventArgs Args;

        public OnDoorPryEvent(InteractUsingEventArgs args)
        {
            Args = args;
        }
    }
}
