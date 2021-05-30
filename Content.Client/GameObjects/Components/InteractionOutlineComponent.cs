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

        private const float DefaultWidth = 1;
        private const string ShaderInRange = "SelectionOutlineInrange";
        private const string ShaderOutOfRange = "SelectionOutline";

        public override string Name => "InteractionOutline";

        private bool _inRange;
        private ShaderInstance? _shader;
        private int _lastRenderScale;

        public void OnMouseEnter(bool inInteractionRange, int renderScale)
        {
            _lastRenderScale = renderScale;
            _inRange = inInteractionRange;
            if (Owner.TryGetComponent(out ISpriteComponent? sprite))
            {
                sprite.PostShader = MakeNewShader(inInteractionRange, renderScale);
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

            _shader?.Dispose();
            _shader = null;
        }

        public void UpdateInRange(bool inInteractionRange, int renderScale)
        {
            if (Owner.TryGetComponent(out ISpriteComponent? sprite)
                && (inInteractionRange != _inRange || _lastRenderScale != renderScale))
            {
                _inRange = inInteractionRange;
                _lastRenderScale = renderScale;
                _shader = MakeNewShader(_inRange, _lastRenderScale);
                sprite.PostShader = _shader;
            }
        }

        private ShaderInstance MakeNewShader(bool inRange, int renderScale)
        {
            var shaderName = inRange ? ShaderInRange : ShaderOutOfRange;

            var instance = _prototypeManager.Index<ShaderPrototype>(shaderName).InstanceUnique();
            instance.SetParameter("outline_width", DefaultWidth * renderScale);
            return instance;
        }
    }
}
