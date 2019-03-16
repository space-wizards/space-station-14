using System;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects;
using SS14.Server.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using SS14.Shared.IoC;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Doors;
using SS14.Shared.Serialization;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.ViewVariables;

namespace Content.Server.GameObjects
{
    public class ServerDoorComponent : Component, IAttackHand
    {
        public override string Name => "Door";
        [ViewVariables]
        public bool Opened { get; private set; }

        private float OpenTimeCounter;

        private CollidableComponent collidableComponent;
        private AppearanceComponent _appearance;

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

        public bool Attackhand(IEntity user)
        {
            if (Opened)
            {
                Close();
            }
            else
            {
                Open();
            }
            return true;
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case BumpedEntMsg msg:
                    if (Opened)
                    {
                        return;
                    }

                    Open();
                    break;
            }
        }

        public void Open()
        {
            Opened = true;
            collidableComponent.IsHardCollidable = false;
            _appearance.SetData(DoorVisuals.VisualState, DoorVisualState.Open);
        }

        public bool Close()
        {
            if (collidableComponent.TryCollision(Vector2.Zero))
            {
                // Do nothing, somebody's in the door.
                return false;
            }
            Opened = false;
            OpenTimeCounter = 0;
            collidableComponent.IsHardCollidable = true;
            _appearance.SetData(DoorVisuals.VisualState, DoorVisualState.Closed);
            return true;
        }

        private const float AUTO_CLOSE_DELAY = 5;
        public void OnUpdate(float frameTime)
        {
            if (!Opened)
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
    }
}
