using Content.Shared.CCVar;
using Content.Client.Hands.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Utility;

namespace Content.Client.CombatMode
{
    public sealed class ShowCombatModeIndicatorsOverlay : Overlay
    {
        private IConfigurationManager _cfg;
        private IInputManager _inputManager;
        private IEntityManager _entMan;
        private IEyeManager _eye;
        private CombatModeSystem _combatSystem;

        public override OverlaySpace Space => OverlaySpace.ScreenSpace;

        private Texture _gunSight;
        private Texture _meleeSight;
        private bool _isShowIndicators;

        public ShowCombatModeIndicatorsOverlay(IConfigurationManager cfg, IInputManager input, IEntityManager entMan,
                IEyeManager eye, CombatModeSystem combatSys)
        {
            IoCManager.InjectDependencies(this);

            _cfg = cfg;
            _inputManager = input;
            _entMan = entMan;
            _eye = eye;
            _combatSystem = combatSys;

            _gunSight = GetTextureFromRsi("gunsight");
            _meleeSight = GetTextureFromRsi("meleesight");

            _isShowIndicators = _cfg.GetCVar(CCVars.HudHeldItemShow);

            _cfg.OnValueChanged(CCVars.HudHeldItemShow,
                isShow => _isShowIndicators = isShow, true);
        }

        private Texture GetTextureFromRsi(string _spriteName)
        {
            var sprite = new SpriteSpecifier.Rsi(
                new ResPath("/Textures/Interface/Misc/pointer_sights.rsi"), _spriteName);
            return _entMan.EntitySysManager.GetEntitySystem<SpriteSystem>().Frame0(sprite);
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (!_isShowIndicators)
                return;

            if (!_combatSystem.IsInCombatMode())
                return;

            var mouseScreen = _inputManager.MouseScreenPosition;
            var mousePosMap = _eye.ScreenToMap(mouseScreen);

            if (mousePosMap.Position == (Vector2)(0, 0))
                return;

            EntityUid? handEntity = _entMan.System<HandsSystem>().GetActiveHandEntity();
            bool isHandGunItem = _entMan.HasComponent<GunComponent>(handEntity);

            var screen = args.ScreenHandle;
            var mousePos = mouseScreen.Position;
            var uiScale = (args.ViewportControl as Control)?.UIScale ?? 1f;
            float limetedScale = uiScale > 1.25f ? 1.25f : uiScale;

            var sight = isHandGunItem ? _gunSight : _meleeSight;
            DrawSight(sight, screen, mousePos, limetedScale);
        }
        private void DrawSight(Texture sight, DrawingHandleScreen screen, Vector2 centerPos, float scale)
        {
            float transparency = 0.4f;

            screen.DrawTextureRect(sight,
                GetRectForTexture(sight.Size, centerPos, scale), Color.Black.WithAlpha(transparency));
            screen.DrawTextureRect(sight,
                GetRectForTexture(sight.Size, centerPos, scale, true), Color.White.WithAlpha(transparency));
        }

        static private UIBox2 GetRectForTexture(Vector2 textureSize, Vector2 pos, float scale,
            bool expanded = false)
        {
            Vector2 sightSize = textureSize * scale;

            if (expanded)
                sightSize += 7;

            return UIBox2.FromDimensions(pos - (sightSize / 2), sightSize);
        }
    }
}
