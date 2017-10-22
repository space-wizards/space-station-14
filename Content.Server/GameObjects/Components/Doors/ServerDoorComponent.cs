using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects;
using SS14.Server.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Log;
using SS14.Shared.Maths;

namespace Content.Server.GameObjects
{
    public class ServerDoorComponent : SharedDoorComponent
    {
        public bool Opened { get; private set; }

        private float OpenTimeCounter;

        private IInteractableComponent interactableComponent;
        private CollidableComponent collidableComponent;

        public override void Initialize()
        {
            base.Initialize();

            interactableComponent = Owner.GetComponent<IInteractableComponent>();
            interactableComponent.OnAttackHand += OnAttackHand;
            collidableComponent = Owner.GetComponent<CollidableComponent>();
            collidableComponent.OnBump += OnBump;
        }

        public override void OnRemove()
        {
            interactableComponent.OnAttackHand -= OnAttackHand;
            interactableComponent = null;
            collidableComponent.OnBump -= OnBump;
            collidableComponent = null;
        }

        private void OnAttackHand(object sender, AttackHandEventArgs args)
        {
            if (Opened)
            {
                Close();
            }
            else
            {
                Open();
            }
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
