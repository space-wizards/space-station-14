using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Client.GameObjects.Components
{
    [RegisterComponent]
    public class InteractionOutlineComponent : Component
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private const string ShaderInRange = "SelectionOutlineInrange";
        private const string ShaderOutOfRange = "SelectionOutline";

        public override string Name => "InteractionOutline";

        private ShaderInstance? _selectionShaderInstance;
        private ShaderInstance? _selectionShaderInRangeInstance;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            _selectionShaderInRangeInstance = _prototypeManager.Index<ShaderPrototype>(ShaderInRange).Instance();
            _selectionShaderInstance = _prototypeManager.Index<ShaderPrototype>(ShaderOutOfRange).Instance();
        }

        public void OnMouseEnter(bool inInteractionRange)
        {
            if (Owner.TryGetComponent(out ISpriteComponent? sprite))
            {
                sprite.PostShader = inInteractionRange ? _selectionShaderInRangeInstance : _selectionShaderInstance;
                sprite.RenderOrder = Owner.EntityManager.CurrentTick.Value;
            }
        }

        public void OnMouseLeave()
        {
            if (Owner.TryGetComponent(out ISpriteComponent? sprite))
            {
                sprite.PostShader = null;
                sprite.RenderOrder = 0;
            }
        }

        public void UpdateInRange(bool inInteractionRange)
        {
            if (Owner.TryGetComponent(out ISpriteComponent? sprite))
            {
                sprite.PostShader = inInteractionRange ? _selectionShaderInRangeInstance : _selectionShaderInstance;
            }
        }
    }
}
