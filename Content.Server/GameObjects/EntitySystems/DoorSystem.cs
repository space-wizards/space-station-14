using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Doors;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    class DoorSystem : EntitySystem
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
        
        private List<ServerDoorComponent> _activeDoors = new List<ServerDoorComponent>();

        public override void Initialize()
        {
            base.Initialize();

            AccessType = AccessTypes.Id;
            SubscribeLocalEvent<DoorStateMessage>(HandleDoorState);
        }

        private void HandleDoorState(DoorStateMessage message)
        {
            switch (message.State)
            {
                case ServerDoorComponent.DoorState.Closed:
                    _activeDoors.Remove(message.Component);
                    break;
                case ServerDoorComponent.DoorState.Open:
                    _activeDoors.Add(message.Component);
                    break;
                case ServerDoorComponent.DoorState.Closing:
                case ServerDoorComponent.DoorState.Opening:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            for (var i = _activeDoors.Count - 1; i >= 0; i--)
            {
                var comp = _activeDoors[i];
                if (comp.Deleted)
                    _activeDoors.RemoveAt(i);
                
                comp.OnUpdate(frameTime);
            }
        }
    }
}
