using System;
using Content.Shared.GameObjects.Components.Doors;
using JetBrains.Annotations;
using Content.Shared.GameObjects.EntitySystems;

namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    /// Used on the server side to automatically close open-doors that auto-close by calling OnUpdate on them.
    /// </summary>
    class ServerDoorSystem : SharedDoorSystem
    {
        /// <summary>
        ///     Determines the base access behavior of all doors on the station.
        /// </summary>
        public AccessTypes AccessType { get; set; }

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

        public override void Initialize()
        {
            base.Initialize();

            AccessType = AccessTypes.Id;
        }

        protected override void HandleDoorState(DoorStateMessage message)
        {
            switch (message.State)
            {
                case SharedDoorComponent.DoorState.Closed:
                    ActiveDoors.Remove(message.Component);
                    break;
                case SharedDoorComponent.DoorState.Open:
                    ActiveDoors.Add(message.Component);
                    break;
                case SharedDoorComponent.DoorState.Closing:
                case SharedDoorComponent.DoorState.Opening:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
