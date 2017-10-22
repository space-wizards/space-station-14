using Content.Server.Interfaces.GameObjects;
using SS14.Server.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.Prototypes
{
    class Door : Entity
    {
        public bool Opened { get; private set; }

        private SpriteComponent spriteComponent;
        private IInteractableComponent interactableComponent;
        private ICollidableComponent collidableComponent;

        public override void Initialize()
        {
            base.Initialize();

            spriteComponent = GetComponent<SpriteComponent>();
            interactableComponent = GetComponent<IInteractableComponent>();
            collidableComponent = GetComponent<ICollidableComponent>();
        }

        public void Open()
        {
            Opened = true;
            spriteComponent.
        }
    }
}
