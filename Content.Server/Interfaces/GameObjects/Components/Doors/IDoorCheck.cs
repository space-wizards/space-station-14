using Content.Shared.GameObjects.Components.Doors;
using Content.Shared.Interfaces.GameObjects.Components;

namespace Content.Server.Interfaces.GameObjects.Components.Doors
{
    public interface IDoorCheck
    {
        /// <summary>
        /// Called when the door's State variable is changed to a new variable that it was not equal to before.
        /// </summary>
        public void OnStateChange(SharedDoorComponent.DoorState doorState) { }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Bool; whether the door should open.</returns>
        public bool OpenCheck() => true;

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Bool; whether the door should close.</returns>
        public bool CloseCheck() => true;

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Bool; whether the door should deny.</returns>
        public bool DenyCheck() => true;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public float? GetPryTime() => null;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public float? GetCloseSpeed() => null;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool BlockActivate(ActivateEventArgs eventArgs) => false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventArgs"></param>
        public void OnStartPry(InteractUsingEventArgs eventArgs) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        public bool CanPryCheck(InteractUsingEventArgs eventArgs) => true;
    }
}
