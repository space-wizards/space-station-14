using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Singularity
{
    public class ContainmentFieldComponent : Component
    {
        public override string Name => "Containment Field";

        private ISpriteComponent _spriteComponent;

        public override void Initialize()
        {
            base.Initialize();

            _spriteComponent = Owner.GetComponent<ISpriteComponent>();

            _spriteComponent.Directional = false;
        }
    }
}
