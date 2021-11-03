using System;
using System.Collections.Generic;
using Content.Shared.Doors;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using static Content.Shared.Doors.SharedDoorComponent;

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

            SubscribeLocalEvent<ClientDoorComponent, DoorStateChangedEvent>(OnDoorStateChanged);
        }

        private void OnDoorStateChanged(EntityUid uid, ClientDoorComponent door, DoorStateChangedEvent args)
        {
            switch (args.State)
            {
                case DoorState.Closed:
                case DoorState.Open:
                    _activeDoors.Remove(door);
                    break;
                case DoorState.Closing:
                case DoorState.Opening:
                    _activeDoors.Add(door);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (!EntityManager.TryGetComponent(uid, out SpriteComponent sprite))
                return;

            // Update sprite draw depth.  If the door is opening or closing, we will use the closed-draw depth.
            sprite.DrawDepth = (args.State == DoorState.Open)
                ? door.OpenDrawDepth
                : door.ClosedDrawDepth;
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
