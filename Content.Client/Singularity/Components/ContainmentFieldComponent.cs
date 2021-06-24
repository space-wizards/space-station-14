using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Content.Client.Singularity.Components
{
    public class ContainmentFieldComponent : Component
    {
        public override string Name => "ContainmentField";

        private SpriteComponent? _spriteComponent;

        protected override void Initialize()
        {
            base.Initialize();

            if (!Owner.TryGetComponent(out _spriteComponent))
            {
                Logger.Error($"{nameof(ContainmentFieldComponent)} created without {nameof(SpriteComponent)}");
            }
            else
            {
                _spriteComponent.Directional = false;
            }
        }
    }
}
