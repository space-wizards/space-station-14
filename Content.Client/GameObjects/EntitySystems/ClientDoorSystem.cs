using System;
using Content.Shared.GameObjects.Components.Doors;
using Content.Shared.GameObjects.EntitySystems;

namespace Content.Client.GameObjects.EntitySystems
{
    /// <summary>
    /// Used on the client side to "predict" when doors will change how collideable they are as part of their opening / closing.
    /// </summary>
    class ClientDoorSystem : SharedDoorSystem
    {
        protected override void HandleDoorState(DoorStateMessage message)
        {
            switch (message.State)
            {
                case SharedDoorComponent.DoorState.Closed:
                case SharedDoorComponent.DoorState.Open:
                    ActiveDoors.Remove(message.Component);
                    break;
                case SharedDoorComponent.DoorState.Closing:
                case SharedDoorComponent.DoorState.Opening:
                    ActiveDoors.Add(message.Component);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
