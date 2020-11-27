using System.Collections.Generic;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Suspicion
{
    public class TraitorOverlay : Overlay
    {
        private readonly IEntityManager _entityManager;
        private readonly IEyeManager _eyeManager;

        public override OverlaySpace Space => OverlaySpace.ScreenSpace;
        private readonly Font _font;

        private readonly IEntity _user;
        private readonly HashSet<EntityUid> _allies = new();
        private readonly string _traitorText = Loc.GetString("Traitor");

        public TraitorOverlay(
            IEntity user,
            IEntityManager entityManager,
            IResourceCache resourceCache,
            IEyeManager eyeManager)
            : base(nameof(TraitorOverlay))
        {
            _entityManager = entityManager;
            _eyeManager = eyeManager;

            _font = new VectorFont(resourceCache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);

            _user = user;
        }

        public bool AddAlly(EntityUid ally)
        {
            return _allies.Add(ally);
        }

        public bool RemoveAlly(EntityUid ally)
        {
            return _allies.Remove(ally);
        }

        protected override void Draw(DrawingHandleBase handle, OverlaySpace currentSpace)
        {
            switch (currentSpace)
            {
                case OverlaySpace.ScreenSpace:
                    DrawScreen((DrawingHandleScreen) handle);
                    break;
            }
        }

        private void DrawScreen(DrawingHandleScreen screen)
        {
            var viewport = _eyeManager.GetWorldViewport();

            foreach (var uid in _allies)
            {
                // Otherwise the entity can not exist yet
                if (!_entityManager.TryGetEntity(uid, out var ally))
                {
                    return;
                }

                if (!ally.TryGetComponent(out IPhysicsComponent physics))
                {
                    return;
                }

                if (!ExamineSystemShared.InRangeUnOccluded(_user.Transform.MapPosition, ally.Transform.MapPosition, 15,
                    entity => entity == _user || entity == ally))
                {
                    return;
                }

                // all entities have a TransformComponent
                var transform = physics.Entity.Transform;

                // if not on the same map, continue
                if (transform.MapID != _eyeManager.CurrentMap || !transform.IsMapTransform)
                {
                    continue;
                }

                var worldBox = physics.WorldAABB;

                // if not on screen, or too small, continue
                if (!worldBox.Intersects(in viewport) || worldBox.IsEmpty())
                {
                    continue;
                }

                var screenCoordinates = _eyeManager.WorldToScreen(physics.WorldAABB.TopLeft + (0, 0.5f));
                DrawString(screen, _font, screenCoordinates, _traitorText, Color.OrangeRed);
            }
        }

        private static void DrawString(DrawingHandleScreen handle, Font font, Vector2 pos, string str, Color color)
        {
            var baseLine = new Vector2(pos.X, font.GetAscent(1) + pos.Y);

            foreach (var chr in str)
            {
                var advance = font.DrawChar(handle, chr, baseLine, 1, color);
                baseLine += new Vector2(advance, 0);
            }
        }
    }
}
