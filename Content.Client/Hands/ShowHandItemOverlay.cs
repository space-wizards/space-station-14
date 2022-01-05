using Content.Shared.CCVar;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.Hands
{
    public sealed class ShowHandItemOverlay : Overlay
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IClyde _clyde = default!;

        private readonly IRenderTexture _renderBackbuffer;

        public override OverlaySpace Space => OverlaySpace.ScreenSpace;

        public ShowHandItemOverlay()
        {
            IoCManager.InjectDependencies(this);

            _renderBackbuffer = _clyde.CreateRenderTarget(
                (64, 64),
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, true),
                new TextureSampleParameters
                {
                    Filter = true
                }, nameof(ShowHandItemOverlay));
        }

        protected override void DisposeBehavior()
        {
            base.DisposeBehavior();

            _renderBackbuffer.Dispose();
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            var sys = EntitySystem.Get<HandsSystem>();
            var handEntity = sys.GetActiveHandEntity();

            if (handEntity == null || !_cfg.GetCVar(CCVars.HudHeldItemShow) || !IoCManager.Resolve<IEntityManager>().HasComponent<ISpriteComponent>(handEntity))
                return;

            var screen = args.ScreenHandle;
            var halfSize = _renderBackbuffer.Size / 2;

            var uiScale = (args.ViewportControl as Control)?.UIScale ?? 1f;

            screen.RenderInRenderTarget(_renderBackbuffer, () =>
            {
                screen.DrawEntity(handEntity.Value, halfSize, new Vector2(1f, 1f) * uiScale, Direction.South);
            }, Color.Transparent);

            var offset = _cfg.GetCVar(CCVars.HudHeldItemOffset);

            var mousePos = _inputManager.MouseScreenPosition.Position;
            screen.DrawTexture(_renderBackbuffer.Texture, mousePos - halfSize + offset, Color.White.WithAlpha(0.75f));
            // screen.DrawRect(UIBox2.FromDimensions((offset, offset) + mousePos, (32, 32)), Color.Red);
        }
    }
}
