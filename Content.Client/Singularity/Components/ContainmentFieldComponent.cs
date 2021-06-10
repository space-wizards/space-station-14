using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Content.Client.Singularity.Components
{
    public class ContainmentFieldComponent : Component
    {
        public override string Name => "Containment Field";

        private SpriteComponent? _spriteComponent;

        public override void Initialize()
        {
            base.Initialize();

            if (!Owner.TryGetComponent(out _spriteComponent))
            {
                Logger.Error("Containmentfieldcomponent created without spritecomponent");
            }
            else
            {
                _spriteComponent.Directional = false;
            }
        }
    }
}
