using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Client.Interactable.Components
{
    [RegisterComponent]
    public sealed class InteractionOutlineComponent : Component
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        private const float DefaultWidth = 1;
        private const string ShaderInRange = "SelectionOutlineInrange";
        private const string ShaderOutOfRange = "SelectionOutline";
        private bool _inRange;
        private ShaderInstance? _shader;
        private int _lastRenderScale;

        public void OnMouseEnter(bool inInteractionRange, int renderScale, SpriteSystem sys)
        {
            _lastRenderScale = renderScale;
            _inRange = inInteractionRange;
            if (_entMan.TryGetComponent(Owner, out SpriteComponent? sprite))
            {
                // TODO why the fuck is this creating a new instance of the outline shader every time the mouse enters???
                sys.SetPostShader(Owner, MakeNewShader(inInteractionRange, renderScale), sprite);
                sprite.RenderOrder = _entMan.CurrentTick.Value;
            }
        }

        public void OnMouseLeave(SpriteSystem sys)
        {
            if (_entMan.TryGetComponent(Owner, out SpriteComponent? sprite))
            {
                sys.SetPostShader(Owner, null, sprite);
                sprite.RenderOrder = 0;
            }

            _shader?.Dispose();
            _shader = null;
        }

        public void UpdateInRange(bool inInteractionRange, int renderScale, SpriteSystem sys)
        {
            if (_entMan.TryGetComponent(Owner, out SpriteComponent? sprite)
                && (inInteractionRange != _inRange || _lastRenderScale != renderScale))
            {
                _inRange = inInteractionRange;
                _lastRenderScale = renderScale;

                _shader = MakeNewShader(_inRange, _lastRenderScale);
                sys.SetPostShader(Owner, _shader, sprite);
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
