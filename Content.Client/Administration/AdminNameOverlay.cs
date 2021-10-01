using System.Collections.Generic;
using Content.Client.Administration.Managers;
using Content.Shared.Administration.Menu;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Client.Administration
{
    internal class AdminNameOverlay : Overlay
    {
        private readonly AdminMenuManager _manager;
        private readonly IEntityManager _entityManager;
        private readonly IEyeManager _eyeManager;
        private IReadOnlyList<AdminMenuPlayerListMessage.PlayerInfo>? _playerInfos;
        private readonly Font _font;

        public AdminNameOverlay(AdminMenuManager manager, IEntityManager entityManager, IEyeManager eyeManager, IResourceCache resourceCache)
        {
            _manager = manager;
            _entityManager = entityManager;
            _eyeManager = eyeManager;
            ZIndex = 200;
            _font = new VectorFont(resourceCache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);
        }

        public override OverlaySpace Space => OverlaySpace.ScreenSpace;

        public void UpdatePlayerInfo(List<AdminMenuPlayerListMessage.PlayerInfo> playerInfos)
        {
            _playerInfos = playerInfos;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (_playerInfos == null)
            {
                return;
            }

            var viewport = _eyeManager.GetWorldViewport();

            foreach (var playerInfo in _playerInfos)
            {
                // Otherwise the entity can not exist yet
                if (!_entityManager.TryGetEntity(playerInfo.EntityUid, out var ally))
                {
                    continue;
                }

                if (!ally.TryGetComponent(out IPhysBody? physics))
                {
                    continue;
                }

                // if not on the same map, continue
                if (physics.Owner.Transform.MapID != _eyeManager.CurrentMap || physics.Owner.IsInContainer())
                {
                    continue;
                }

                var worldBox = physics.GetWorldAABB();

                // if not on screen, or too small, continue
                if (!worldBox.Intersects(in viewport) || worldBox.IsEmpty())
                {
                    continue;
                }

                var lineoffset = new Vector2(0, 11f);
                var screenCoordinates = _eyeManager.WorldToScreen(physics.GetWorldAABB().TopRight + (0, -0.1f));
                if (playerInfo.Antag || true)
                {
                    args.ScreenHandle.DrawString(_font, screenCoordinates + (lineoffset * 2), "ANTAG", Color.OrangeRed);
                }
                args.ScreenHandle.DrawString(_font, screenCoordinates+(lineoffset), playerInfo.Username, Color.Yellow);
                args.ScreenHandle.DrawString(_font, screenCoordinates, playerInfo.CharacterName, Color.Aquamarine);
            }
        }
    }
}
