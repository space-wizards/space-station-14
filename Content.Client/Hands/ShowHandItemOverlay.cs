using System.Numerics;
using Content.Client.Hands.Systems;
using Content.Shared.CCVar;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Direction = Robust.Shared.Maths.Direction;

namespace Content.Client.Hands
{
    public sealed class ShowHandItemOverlay : Overlay
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IClyde _clyde = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        private HandsSystem? _hands;
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

        protected override bool BeforeDraw(in OverlayDrawArgs args)
        {
            if (!_cfg.GetCVar(CCVars.HudHeldItemShow))
                return false;

            return base.BeforeDraw(in args);
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            var mousePos = _inputManager.MouseScreenPosition;

            // Offscreen
            if (mousePos.Window == WindowId.Invalid)
                return;

            var screen = args.ScreenHandle;
            var offset = _cfg.GetCVar(CCVars.HudHeldItemOffset);
            var offsetVec = new Vector2(offset, offset);

            if (IconOverride != null)
            {
                screen.DrawTexture(IconOverride, mousePos.Position - IconOverride.Size / 2 + offsetVec, Color.White.WithAlpha(0.75f));
                return;
            }

            _hands ??= _entMan.System<HandsSystem>();
            var handEntity = _hands.GetActiveHandEntity();

            if (handEntity == null || !_entMan.TryGetComponent(handEntity, out SpriteComponent? sprite))
                return;

            var halfSize = _renderBackbuffer.Size / 2;
            var uiScale = (args.ViewportControl as Control)?.UIScale ?? 1f;

            screen.RenderInRenderTarget(_renderBackbuffer, () =>
            {
                screen.DrawEntity(handEntity.Value, halfSize, new Vector2(1f, 1f) * uiScale, Angle.Zero, Angle.Zero, Direction.South, sprite);
            }, Color.Transparent);

            screen.DrawTexture(_renderBackbuffer.Texture, mousePos.Position - halfSize + offsetVec, Color.White.WithAlpha(0.75f));
        }
    }
}
