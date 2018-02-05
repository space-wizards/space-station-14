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

namespace Content.Server.GameObjects
{
    public class ServerDoorComponent : SharedDoorComponent, IAttackHand
    {
        public bool Opened { get; private set; }

        private float OpenTimeCounter;
        
        private CollidableComponent collidableComponent;

        public override void Initialize()
        {
            base.Initialize();

            collidableComponent = Owner.GetComponent<CollidableComponent>();
            collidableComponent.OnBump += OnBump;
        }

        public override void OnRemove()
        {
            collidableComponent.OnBump -= OnBump;
            collidableComponent = null;
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

        private void OnBump(object sender, BumpEventArgs args)
        {
            Logger.Info("Bump!");
            if (Opened)
            {
                return;
            }

            Open();
        }

        public void Open()
        {
            Opened = true;
            collidableComponent.IsHardCollidable = false;
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
            return true;
        }

        public override ComponentState GetComponentState()
        {
            return new DoorComponentState(Opened);
        }

        private const float AUTO_CLOSE_DELAY = 5;
        public override void Update(float frameTime)
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
