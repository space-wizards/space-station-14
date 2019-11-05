using System;
using Content.Server.GameObjects.Components.Access;
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

        protected virtual DoorState State
        {
            get => _state;
            set => _state = value;
        }

        private float OpenTimeCounter;

        private CollidableComponent collidableComponent;
        private AppearanceComponent _appearance;

        private static readonly TimeSpan CloseTime = TimeSpan.FromSeconds(1.2f);
        private static readonly TimeSpan OpenTimeOne = TimeSpan.FromSeconds(0.3f);
        private static readonly TimeSpan OpenTimeTwo = TimeSpan.FromSeconds(0.9f);
        private static readonly TimeSpan DenyTime = TimeSpan.FromSeconds(0.45f);

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

        protected virtual void ActivateImpl(ActivateEventArgs eventArgs)
        {
            if (State == DoorState.Open)
            {
                TryClose(eventArgs.User);
            }
            else if (State == DoorState.Closed)
            {
                TryOpen(eventArgs.User);
            }
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            ActivateImpl(eventArgs);
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case BumpedEntMsg msg:
                    if (State != DoorState.Closed)
                    {
                        return;
                    }

                    // Only open when bumped by mobs.
                    if (!msg.Entity.HasComponent(typeof(SpeciesComponent)))
                    {
                        return;
                    }

                    TryOpen(msg.Entity);
                    break;
            }
        }

        public virtual bool CanOpen()
        {
            return true;
        }

        public bool CanOpen(IEntity user)
        {
            if (!CanOpen()) return false;
            if (!Owner.TryGetComponent(out AccessReader accessReader))
            {
                return true;
            }
            return accessReader.IsAllowed(user);
        }

        public void TryOpen(IEntity user)
        {
            if (!CanOpen(user))
            {
                Deny();
                return;
            }
            Open();
        }

        public void Open()
        {
            if (State != DoorState.Closed)
            {
                return;
            }

            State = DoorState.Opening;
            _appearance.SetData(DoorVisuals.VisualState, DoorVisualState.Opening);

            Timer.Spawn(OpenTimeOne, async () =>
            {
                collidableComponent.IsHardCollidable = false;

                await Timer.Delay(OpenTimeTwo);

                State = DoorState.Open;
                _appearance.SetData(DoorVisuals.VisualState, DoorVisualState.Open);
            });
        }

        public virtual bool CanClose()
        {
            return true;
        }

        public bool CanClose(IEntity user)
        {
            if (!CanClose()) return false;
            if (!Owner.TryGetComponent(out AccessReader accessReader))
            {
                return true;
            }
            return accessReader.IsAllowed(user);
        }

        public void TryClose(IEntity user)
        {
            if (!CanClose(user))
            {
                Deny();
                return;
            }
            Close();
        }

        public bool Close()
        {
            if (collidableComponent.TryCollision(Vector2.Zero))
            {
                // Do nothing, somebody's in the door.
                return false;
            }

            State = DoorState.Closing;
            collidableComponent.IsHardCollidable = true;
            OpenTimeCounter = 0;
            _appearance.SetData(DoorVisuals.VisualState, DoorVisualState.Closing);

            Timer.Spawn(CloseTime, () =>
            {
                State = DoorState.Closed;
                _appearance.SetData(DoorVisuals.VisualState, DoorVisualState.Closed);
            });
            return true;
        }

        public virtual void Deny()
        {
            _appearance.SetData(DoorVisuals.VisualState, DoorVisualState.Deny);
            Timer.Spawn(DenyTime, () =>
            {
                _appearance.SetData(DoorVisuals.VisualState, DoorVisualState.Closed);
            });
        }

        private const float AUTO_CLOSE_DELAY = 5;
        public virtual void OnUpdate(float frameTime)
        {
            if (State != DoorState.Open)
            {
                return;
            }

            OpenTimeCounter += frameTime;
            if (OpenTimeCounter > AUTO_CLOSE_DELAY)
            {
                if (!CanClose() || !Close())
                {
                    // Try again in 2 seconds if it's jammed or something.
                    OpenTimeCounter -= 2;
                }
            }
        }

        protected enum DoorState
        {
            Closed,
            Open,
            Closing,
            Opening,
        }
    }
}
