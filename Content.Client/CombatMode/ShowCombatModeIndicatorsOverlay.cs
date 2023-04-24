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
using Robust.Shared.Map;

namespace Content.Client.CombatMode
{
    public sealed class ShowCombatModeIndicatorsOverlay : Overlay
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IClyde _clyde = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IEyeManager _eye = default!;
        private readonly IRenderTexture _renderBackbuffer;
        private Texture? _gunSight;
        private Texture? _meleeSight;
        private CombatModeSystem _combatSystem;
        private IPlayerManager _player;
        public override OverlaySpace Space => OverlaySpace.ScreenSpace;
        public ShowCombatModeIndicatorsOverlay(IPlayerManager playerManager, CombatModeSystem combatSys)
        {
            IoCManager.InjectDependencies(this);

            _combatSystem = combatSys;
            _player = playerManager;

            _gunSight = GetTextureFromRsi("gunsight");
            _meleeSight = GetTextureFromRsi("meleesight");

            _renderBackbuffer = _clyde.CreateRenderTarget(
                (100, 100),
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, true),
                new TextureSampleParameters
                {
                    Filter = true
                }, nameof(ShowCombatModeIndicatorsOverlay));
        }

        private Texture GetTextureFromRsi(string _spriteName)
        {
            var sprite = new SpriteSpecifier.Rsi(
                new ResPath("/Textures/Interface/Misc/pointer_sights.rsi"), _spriteName);
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
            var halfBufferSize = _renderBackbuffer.Size / 2;

            screen.RenderInRenderTarget(_renderBackbuffer, () =>
            {
                if (isHandGunItem)
                    DrawSight(_gunSight, screen, halfBufferSize, limetedScale);
                else
                    DrawSight(_meleeSight, screen, halfBufferSize, limetedScale);
            }, Color.Transparent);

            screen.DrawTexture(_renderBackbuffer.Texture,
                mousePos - halfBufferSize, Color.White.WithAlpha(0.9f));
        }
        private void DrawSight(Texture? sight, DrawingHandleScreen screen, Vector2 centerPos, float scale)
        {
            if (sight == null)
                return;

            Vector2 sightSize = sight.Size * scale;
            Vector2 halfSightSize = sightSize / 2;

            Vector2 beginPosSight = centerPos - halfSightSize;
            UIBox2 coordsRect = UIBox2.FromDimensions(beginPosSight, sightSize);

            screen.DrawTextureRect(sight, coordsRect, Color.Black);
        }
    }
}
