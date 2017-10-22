using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects;
using SS14.Server.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Log;

namespace Content.Server.GameObjects
{
    public class ServerDoorComponent : SharedDoorComponent
    {
        public bool Opened { get; private set; }

        private IInteractableComponent interactableComponent;
        private CollidableComponent collidableComponent;

        public override void Initialize()
        {
            base.Initialize();

            interactableComponent = Owner.GetComponent<IInteractableComponent>();
            interactableComponent.OnAttackHand += OnAttackHand;
            collidableComponent = Owner.GetComponent<CollidableComponent>();
        }

        public override void OnRemove()
        {
            interactableComponent.OnAttackHand -= OnAttackHand;
            interactableComponent = null;
            collidableComponent = null;
        }

        private void OnAttackHand(object sender, AttackHandEventArgs args)
        {
            Logger.Info("Yes!");
            if (Opened)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            Opened = true;
            //collidableComponent.IsHardCollidable = false;
        }

        public void Close()
        {
            Opened = false;
            //collidableComponent.IsHardCollidable = true;
        }

        public override ComponentState GetComponentState()
        {
            return new DoorComponentState(Opened);
        }
    }
}
