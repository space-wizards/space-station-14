using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Content.Client.GameObjects.Components.Singularity
{
    [UsedImplicitly]
    public class ContainmentFieldComponent : Component
    {
        public override string Name => "Containment Field";

        private SpriteComponent _spriteComponent;

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
