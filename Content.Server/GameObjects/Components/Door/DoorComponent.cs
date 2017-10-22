using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects.Components.Door;
using SS14.Server.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;

namespace Content.Server.GameObjects.Components.Door
{
    class DoorComponent : SharedDoorComponent
    {
        public bool Opened { get; private set; }

        private SpriteComponent spriteComponent;
        private IInteractableComponent interactableComponent;
        private ICollidableComponent collidableComponent;

        public override void Initialize()
        {
            base.Initialize();

            spriteComponent = Owner.GetComponent<SpriteComponent>();
            interactableComponent = Owner.GetComponent<IInteractableComponent>();
            collidableComponent = Owner.GetComponent<ICollidableComponent>();
        }

        public void Open()
        {
            Opened = true;
            spriteComponent.
        }
    }
}
