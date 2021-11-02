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
            if (!EntityManager.TryGetComponent(uid, out SpriteComponent sprite))
                return;

            if (args.State == DoorState.Open || args.State == DoorState.Closed)
                _activeDoors.Remove(door);

            // set draw depth to open if and only if the state is currently open (not opening or closing).
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
