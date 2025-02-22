using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Controls
{
    /// <summary>
    ///     Copy-paste of engine <see cref="AnimatedTextureRect"/> that can be scaled.
    /// </summary>
    public sealed class ScaledAnimatedTextureRect : Control
    {
        private IRsiStateLike? _state;
        private int _curFrame;
        private float _curFrameTime;

        /// <summary>
        ///     Internal TextureRect used to do actual drawing of the texture.
        ///     You can use this property to change shaders or styling or such.
        /// </summary>
        public TextureRect DisplayRect { get; }

        public RsiDirection RsiDirection { get; } = RsiDirection.South;

        public ScaledAnimatedTextureRect()
        {
            IoCManager.InjectDependencies(this);

            DisplayRect = new TextureRect()
            {
                TextureScale = new(1, 1)
            };
            AddChild(DisplayRect);
        }

        public void SetFromSpriteSpecifier(SpriteSpecifier specifier, Vector2 scale)
        {
            _curFrame = 0;
            _state = specifier.RsiStateLike();
            _curFrameTime = _state.GetDelay(0);
            DisplayRect.Texture = _state.GetFrame(RsiDirection, 0);
            DisplayRect.TextureScale = scale;
        }

        public void SetScale(Vector2 scale)
        {
            DisplayRect.TextureScale = scale;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            if (!VisibleInTree || _state == null || !_state.IsAnimated)
                return;

            var oldFrame = _curFrame;

            _curFrameTime -= args.DeltaSeconds;
            while (_curFrameTime < _state.GetDelay(_curFrame))
            {
                _curFrame = (_curFrame + 1) % _state.AnimationFrameCount;
                _curFrameTime += _state.GetDelay(_curFrame);
            }

            if (_curFrame != oldFrame)
            {
                DisplayRect.Texture = _state.GetFrame(RsiDirection, _curFrame);
            }
        }
    }
}
