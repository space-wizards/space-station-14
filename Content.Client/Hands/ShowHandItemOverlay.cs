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

        private float _handIconOffset;

        public override OverlaySpace Space => OverlaySpace.ScreenSpace;

        public Texture? IconOverride;
        public EntityUid? EntityOverride;

        public ShowHandItemOverlay()
        {
            IoCManager.InjectDependencies(this);

            _handIconOffset = _cfg.GetCVar(CCVars.HudHeldItemOffset);

            _renderBackbuffer = _clyde.CreateRenderTarget(
                (120, 120),
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
            var mousePos = _inputManager.MouseScreenPosition.Position;

            if (IsIfDrawIconOverride(screen, mousePos))
                return;

            var handEntity = EntityOverride ?? EntitySystem.Get<HandsSystem>().GetActiveHandEntity();

            if (handEntity == null || !_entMan.HasComponent<SpriteComponent>(handEntity))
                return;

            // if (handEntity != null) {
            //     _entMan.TryGetComponent<MetaDataComponent>(handEntity, out var metaDat);
            // }

            var uiScale = (args.ViewportControl as Control)?.UIScale ?? 1f;
            var bufferCenterOffset = _renderBackbuffer.Size / 2;

            screen.RenderInRenderTarget(_renderBackbuffer, () =>
            {
                DrawWeaponSight(screen, bufferCenterOffset, uiScale);
                DrawHandEntityIcon(screen, handEntity.Value, bufferCenterOffset, uiScale);
            }, Color.Transparent);

            screen.DrawTexture(_renderBackbuffer.Texture,
                mousePos - bufferCenterOffset, Color.White.WithAlpha(0.8f));
        }

        private bool IsIfDrawIconOverride(DrawingHandleScreen screen, Vector2 mousePos)
        {
            if (IconOverride != null)
            {
                screen.DrawTexture(IconOverride, mousePos - IconOverride.Size / 2 + _handIconOffset,
                    Color.White.WithAlpha(0.75f));
                return true;
            }
            else
            {
                return false;
            }
        }

        private void DrawHandEntityIcon(DrawingHandleScreen screen, EntityUid handEntity,
            Vector2 centerPos, float scale)
        {
            Vector2 offset = centerPos + _handIconOffset;
            screen.DrawCircle(offset, offset.X / 4.5f * scale, Color.White.WithAlpha(0.02f));
            screen.DrawEntity(handEntity, offset, new Vector2(1f, 1f) * scale, Direction.South);
        }

        private void DrawWeaponSight(DrawingHandleScreen screen, Vector2 centerPos, float scale)
        {
            float circleRadious = centerPos.X / 3f * scale;
            screen.DrawCircle(centerPos, circleRadious, Color.Purple, false);
            screen.DrawCircle(centerPos, (circleRadious) + 1, Color.Purple, false);
            screen.DrawCircle(centerPos, (circleRadious) + 2, Color.Purple, false);
            screen.DrawCircle(centerPos, (circleRadious) + 3, Color.Purple, false);
            screen.DrawCircle(centerPos, (circleRadious) + 4, Color.Red, false);
        }
    }
}
