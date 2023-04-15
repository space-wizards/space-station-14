using Content.Client.Hands.Systems;
using Content.Shared.CCVar;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Content.Shared.Weapons.Ranged.Components;
using Content.Client.CombatMode;
using Robust.Shared.Utility;

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

        private Texture? _gunSight;
        private Texture? _meleeSight;

        public override OverlaySpace Space => OverlaySpace.ScreenSpace;

        public Texture? IconOverride;
        public EntityUid? EntityOverride;

        public ShowHandItemOverlay()
        {
            IoCManager.InjectDependencies(this);

            _handIconOffset = _cfg.GetCVar(CCVars.HudHeldItemOffset);

            _renderBackbuffer = _clyde.CreateRenderTarget(
                (100, 100),
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, true),
                new TextureSampleParameters
                {
                    Filter = true
                }, nameof(ShowHandItemOverlay));

            _gunSight = GetTextureFromRsi("gun-sight");
            _meleeSight = GetTextureFromRsi("melee-sight");
        }

        private Texture GetTextureFromRsi(string _spriteName)
        {
            var sprite = new SpriteSpecifier.Rsi(
                new ResourcePath("/Textures/Interface/Misc/pointer_sights.rsi"), _spriteName);
            return _entMan.EntitySysManager.GetEntitySystem<SpriteSystem>().Frame0(sprite);
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

            EntityUid? handEntity = null;

            if (IconOverride != null)
                DrawIconOverride(IconOverride, screen, mousePos);
            else
                handEntity = EntityOverride ?? EntitySystem.Get<HandsSystem>().GetActiveHandEntity();

            bool isHandItemSprite = handEntity != null && _entMan.HasComponent<SpriteComponent>(handEntity);

            var combatSystem = EntitySystem.Get<CombatModeSystem>();
            bool isCombatMode = combatSystem != null && combatSystem.IsInCombatMode();

            if (!isHandItemSprite && !isCombatMode)
                return;

            bool isHandGunItem = _entMan.HasComponent<GunComponent>(handEntity);

            var uiScale = (args.ViewportControl as Control)?.UIScale ?? 1f;
            float limetedScale = uiScale > 1.25f ? 1.25f : uiScale;
            var halfBufferSize = _renderBackbuffer.Size / 2;

            screen.RenderInRenderTarget(_renderBackbuffer, () =>
            {
                if (isCombatMode)
                {
                    if (isHandGunItem)
                        DrawSight(_gunSight, screen, halfBufferSize, limetedScale);
                    else
                        DrawSight(_meleeSight, screen, halfBufferSize, limetedScale);
                }

                if (isHandItemSprite && handEntity != null)
                    DrawHandEntityIcon(screen, handEntity.Value, halfBufferSize, limetedScale);

            }, Color.Transparent);

            screen.DrawTexture(_renderBackbuffer.Texture,
                mousePos - halfBufferSize, Color.White.WithAlpha(0.75f));
        }

        private void DrawIconOverride(Texture icon, DrawingHandleScreen screen, Vector2 mousePos)
        {
            screen.DrawTexture(icon, mousePos - icon.Size / 2 + _handIconOffset,
                Color.White.WithAlpha(0.75f));
        }

        private void DrawHandEntityIcon(DrawingHandleScreen screen, EntityUid handEntity,
            Vector2 centerPos, float scale)
        {
            screen.DrawEntity(handEntity, (centerPos + _handIconOffset),
                new Vector2(1f, 1f) * scale, Direction.South);
        }

        private void DrawSight(Texture? sight, DrawingHandleScreen screen, Vector2 centerPos, float scale)
        {
            if (sight == null)
                return;

            Vector2 sightSize = sight.Size * scale;
            Vector2 halfSightSize = sightSize / 2;

            Vector2 beginPosSight = centerPos - halfSightSize;
            UIBox2 coordsRect = UIBox2.FromDimensions(beginPosSight, sightSize);
            screen.DrawTextureRect(sight, coordsRect, Color.DarkBlue);
        }
    }
}
