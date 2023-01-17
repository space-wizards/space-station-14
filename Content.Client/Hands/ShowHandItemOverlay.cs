using Content.Client.Hands.Systems;
using Content.Shared.CCVar;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;

namespace Content.Client.Hands
{
    public sealed class ShowHandItemOverlay : Overlay
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IClyde _clyde = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        private readonly IRenderTexture _renderBackbuffer;

        public override OverlaySpace Space => OverlaySpace.ScreenSpace;

        public Texture? IconOverride;
        public EntityUid? EntityOverride;

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
            if (!_cfg.GetCVar(CCVars.HudHeldItemShow))
                return;

            var screen = args.ScreenHandle;
            var offset = _cfg.GetCVar(CCVars.HudHeldItemOffset);
            var mousePos = _inputManager.MouseScreenPosition.Position;

            if (IconOverride != null)
            {
                screen.DrawTexture(IconOverride, mousePos - IconOverride.Size / 2 + offset, Color.White.WithAlpha(0.75f));
                return;
            }

            var handEntity = EntityOverride ?? EntitySystem.Get<HandsSystem>().GetActiveHandEntity();

            if (handEntity == null || !_entMan.HasComponent<SpriteComponent>(handEntity))
                return;

            var halfSize = _renderBackbuffer.Size / 2;
            var uiScale = (args.ViewportControl as Control)?.UIScale ?? 1f;

            screen.RenderInRenderTarget(_renderBackbuffer, () =>
            {
                screen.DrawEntity(handEntity.Value, halfSize, new Vector2(1f, 1f) * uiScale, Direction.South);
            }, Color.Transparent);

            screen.DrawTexture(_renderBackbuffer.Texture, mousePos - halfSize + offset, Color.White.WithAlpha(0.75f));
        }
    }
}
