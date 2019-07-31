using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Doors;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Maths;
using Robust.Shared.Timers;

namespace Content.Server.GameObjects
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class ServerDoorComponent : Component, IActivate
    {
        public override string Name => "Door";

        private DoorState _state = DoorState.Closed;

        private float OpenTimeCounter;

        private CollidableComponent collidableComponent;
        private AppearanceComponent _appearance;

        private static readonly TimeSpan CloseTime = TimeSpan.FromSeconds(1.2f);
        private static readonly TimeSpan OpenTimeOne = TimeSpan.FromSeconds(0.3f);
        private static readonly TimeSpan OpenTimeTwo = TimeSpan.FromSeconds(0.9f);

        public override void Initialize()
        {
            base.Initialize();

            collidableComponent = Owner.GetComponent<CollidableComponent>();
            _appearance = Owner.GetComponent<AppearanceComponent>();
        }

        public override void OnRemove()
        {
            collidableComponent = null;
            _appearance = null;

            base.OnRemove();
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (_state == DoorState.Open)
            {
                Close();
            }
            else if (_state == DoorState.Closed)
            {
                Open();
            }
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case BumpedEntMsg msg:
                    if (_state != DoorState.Closed)
                    {
                        return;
                    }

                    // Only open when bumped by mobs.
                    if (!msg.Entity.HasComponent(typeof(SpeciesComponent)))
                    {
                        return;
                    }

                    Open();
                    break;
            }
        }

        public void Open()
        {
            if (_state != DoorState.Closed)
            {
                return;
            }

            _state = DoorState.Opening;
            _appearance.SetData(DoorVisuals.VisualState, DoorVisualState.Opening);

            Timer.Spawn(OpenTimeOne, async () =>
            {
                collidableComponent.IsHardCollidable = false;

                await Timer.Delay(OpenTimeTwo);

                _state = DoorState.Open;
                _appearance.SetData(DoorVisuals.VisualState, DoorVisualState.Open);
            });
        }

        public bool Close()
        {
            if (collidableComponent.TryCollision(Vector2.Zero))
            {
                // Do nothing, somebody's in the door.
                return false;
            }

            _state = DoorState.Closing;
            collidableComponent.IsHardCollidable = true;
            OpenTimeCounter = 0;
            _appearance.SetData(DoorVisuals.VisualState, DoorVisualState.Closing);

            Timer.Spawn(CloseTime, () =>
            {
                _state = DoorState.Closed;
                _appearance.SetData(DoorVisuals.VisualState, DoorVisualState.Closed);
            });
            return true;
        }

        private const float AUTO_CLOSE_DELAY = 5;
        public void OnUpdate(float frameTime)
        {
            if (_state != DoorState.Open)
            {
                return;
            }

            OpenTimeCounter += frameTime;
            if (OpenTimeCounter > AUTO_CLOSE_DELAY)
            {
                if (!Close())
                {
                    // Try again in 2 seconds if it's jammed or something.
                    OpenTimeCounter -= 2;
                }
            }
        }

        private enum DoorState
        {
            Closed,
            Open,
            Closing,
            Opening,
        }
    }
}
