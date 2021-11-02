using System;
using System.Collections.Generic;
using Content.Shared.Doors;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Doors
{
    /// <summary>
    /// Used by the client to "predict" when doors will change how collideable they are as part of their opening / closing.
    /// </summary>
    internal sealed class DoorSystem : SharedDoorSystem
    {
        /// <summary>
        /// List of doors that need to be periodically checked.
        /// </summary>
        private readonly List<ClientDoorComponent> _activeDoors = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DoorStateMessage>(HandleDoorState);

            SubscribeLocalEvent<ClientDoorComponent, DoorStateChangedEvent>(OnDoorStateChanged);
        }

        private void OnDoorStateChanged(EntityUid uid, ClientDoorComponent door, DoorStateChangedEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out SpriteComponent sprite))
                return;

            // set draw depth to open if and only if the state is currently open (not opening or closing).
            sprite.DrawDepth = (args.State == DoorState.Open)
                ? door.OpenDrawDepth
                : door.ClosedDrawDepth;
        }

        /// <summary>
        /// Registers doors to be periodically checked.
        /// </summary>
        /// <param name="message">A message corresponding to the component under consideration, raised when its state changes.</param>
        private void HandleDoorState(DoorStateMessage message)
        {
            switch (message.State)
            {
                case SharedDoorComponent.DoorState.Closed:
                case SharedDoorComponent.DoorState.Open:
                    _activeDoors.Remove(message.Component);
                    break;
                case SharedDoorComponent.DoorState.Closing:
                case SharedDoorComponent.DoorState.Opening:
                    _activeDoors.Add(message.Component);
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
                {
                    _activeDoors.RemoveAt(i);
                }
                comp.OnUpdate();
            }
        }
    }
}
