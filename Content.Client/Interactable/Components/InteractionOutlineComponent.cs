using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Interactable.Components
{
    [RegisterComponent]
    public sealed partial class InteractionOutlineComponent : Component
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        private const float DefaultWidth = 1;

        [ValidatePrototypeId<ShaderPrototype>]
        private const string ShaderInRange = "SelectionOutlineInrange";

        [ValidatePrototypeId<ShaderPrototype>]
        private const string ShaderOutOfRange = "SelectionOutline";

        private bool _inRange;
        private ShaderInstance? _shader;
        private int _lastRenderScale;

        public void OnMouseEnter(bool inInteractionRange, int renderScale)
        {
            _lastRenderScale = renderScale;
            _inRange = inInteractionRange;
            if (_entMan.TryGetComponent(Owner, out SpriteComponent? sprite) && sprite.PostShader == null)
            {
                // TODO why is this creating a new instance of the outline shader every time the mouse enters???
                _shader = MakeNewShader(inInteractionRange, renderScale);
                sprite.PostShader = _shader;
            }
        }

        public void OnMouseLeave()
        {
            if (_entMan.TryGetComponent(Owner, out SpriteComponent? sprite))
            {
                if (sprite.PostShader == _shader)
                    sprite.PostShader = null;
                sprite.RenderOrder = 0;
            }

            _shader?.Dispose();
            _shader = null;
        }

        public void UpdateInRange(bool inInteractionRange, int renderScale)
        {
            if (_entMan.TryGetComponent(Owner, out SpriteComponent? sprite)
                && sprite.PostShader == _shader
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
