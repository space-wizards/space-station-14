using System;
using Content.Shared.Doors;
using Content.Shared.Interaction;

namespace Content.Server.Doors
{
    public interface IDoorCheck
    {
        /// <summary>
        /// Called when the door's State variable is changed to a new variable that it was not equal to before.
        /// </summary>
        void OnStateChange(SharedDoorComponent.DoorState doorState) { }

        /// <summary>
        /// Called when the door is determining whether it is able to open.
        /// </summary>
        /// <returns>True if the door should open, false if it should not.</returns>
        bool OpenCheck() => true;

        /// <summary>
        /// Called when the door is determining whether it is able to close.
        /// </summary>
        /// <returns>True if the door should close, false if it should not.</returns>
        bool CloseCheck() => true;

        /// <summary>
        /// Called when the door is determining whether it is able to deny.
        /// </summary>
        /// <returns>True if the door should deny, false if it should not.</returns>
        bool DenyCheck() => true;

        /// <summary>
        /// Whether the door's safety is on.
        /// </summary>
        /// <returns>True if safety is on, false if it is not.</returns>
        bool SafetyCheck() => false;

        /// <summary>
        /// Whether the door should close automatically.
        /// </summary>
        /// <returns>True if the door should close automatically, false if it should not.</returns>
        bool AutoCloseCheck() => false;

        /// <summary>
        /// Gets an override for the amount of time to pry open the door, or null if there is no override.
        /// </summary>
        /// <returns>Float if there is an override, null otherwise.</returns>
        float? GetPryTime() => null;

        /// <summary>
        /// Gets an override for the amount of time before the door automatically closes, or null if there is no override.
        /// </summary>
        /// <returns>TimeSpan if there is an override, null otherwise.</returns>
        TimeSpan? GetCloseSpeed() => null;

        /// <summary>
        /// A check to determine whether or not a click on the door should interact with it with the intent to open/close.
        /// </summary>
        /// <returns>True if the door's IActivate should not run, false otherwise.</returns>
        bool BlockActivate(ActivateEventArgs eventArgs) => false;

        /// <summary>
        /// Called when somebody begins to pry open the door.
        /// </summary>
        /// <param name="eventArgs">The eventArgs of the InteractUsing method that called this function.</param>
        void OnStartPry(InteractUsingEventArgs eventArgs) { }

        /// <summary>
        /// Check representing whether or not the door can be pried open.
        /// </summary>
        /// <param name="eventArgs">The eventArgs of the InteractUsing method that called this function.</param>
        /// <returns>True if the door can be pried open, false if it cannot.</returns>
        bool CanPryCheck(InteractUsingEventArgs eventArgs) => true;

    }
}
