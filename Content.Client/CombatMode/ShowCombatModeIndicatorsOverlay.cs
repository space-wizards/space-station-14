using Content.Shared.CCVar;
using Content.Client.Hands.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.Player;
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
        private IPlayerManager _player;
        public override OverlaySpace Space => OverlaySpace.ScreenSpace;
        private Texture _gunSight;
        private Texture _meleeSight;

        public ShowCombatModeIndicatorsOverlay(IPlayerManager playerManager,
            IConfigurationManager cfg, IInputManager input, IEntityManager entMan,
                IEyeManager eye, CombatModeSystem combatSys)
        {
            IoCManager.InjectDependencies(this);

            _player = playerManager;
            _cfg = cfg;
            _inputManager = input;
            _entMan = entMan;
            _eye = eye;
            _combatSystem = combatSys;

            _gunSight = GetTextureFromRsi("gunsight");
            _meleeSight = GetTextureFromRsi("meleesight");
        }

        private Texture GetTextureFromRsi(string _spriteName)
        {
            var sprite = new SpriteSpecifier.Rsi(
                new ResPath("/Textures/Interface/Misc/pointer_sights.rsi"), _spriteName);
            return _entMan.EntitySysManager.GetEntitySystem<SpriteSystem>().Frame0(sprite);
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (!_cfg.GetCVar(CCVars.HudHeldItemShow))
                return;

            var player = _player.LocalPlayer?.ControlledEntity;

            if (player == null ||
                !_entMan.TryGetComponent<TransformComponent>(player, out var xform))
            {
                return;
            }

            if (!_combatSystem.IsInCombatMode(player.Value))
                return;

            var xMapPos = xform.MapPosition;

            var mouseScreen = _inputManager.MouseScreenPosition;
            var mousePosMap = _eye.ScreenToMap(mouseScreen);

            if (xMapPos.MapId != mousePosMap.MapId)
                return;

            EntityUid? handEntity = EntitySystem.Get<HandsSystem>().GetActiveHandEntity();
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
            float transparency = 0.55f;

            screen.DrawTextureRect(sight,
                GetRectForTexture(sight, centerPos, scale), Color.Black.WithAlpha(transparency));
            screen.DrawTextureRect(sight,
                GetRectForTexture(sight, centerPos, scale, true), Color.White.WithAlpha(transparency));
        }

        static private UIBox2 GetRectForTexture(Texture texture, Vector2 pos, float scale,
            bool expanded = false)
        {
            Vector2 sightSize = texture.Size * scale;

            if (expanded)
                sightSize += 7;

            Vector2 halfSightSize = sightSize / 2;

            Vector2 beginPosSight = pos - halfSightSize;
            return UIBox2.FromDimensions(beginPosSight, sightSize);
        }
    }
}
