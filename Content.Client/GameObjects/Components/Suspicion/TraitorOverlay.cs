using Content.Shared.GameObjects.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Client.GameObjects.Components.Suspicion
{
    public class TraitorOverlay : Overlay
    {
        private readonly IEntityManager _entityManager;
        private readonly IEyeManager _eyeManager;
        private readonly IPlayerManager _playerManager;

        public override OverlaySpace Space => OverlaySpace.ScreenSpace;
        private readonly Font _font;

        private readonly string _traitorText = Loc.GetString("Traitor");

        public TraitorOverlay(
            IEntityManager entityManager,
            IResourceCache resourceCache,
            IEyeManager eyeManager)
        {
            _playerManager = IoCManager.Resolve<IPlayerManager>();

            _entityManager = entityManager;
            _eyeManager = eyeManager;

            _font = new VectorFont(resourceCache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            var viewport = _eyeManager.GetWorldViewport();

            var ent = _playerManager.LocalPlayer?.ControlledEntity;
            if (ent == null || ent.TryGetComponent(out SuspicionRoleComponent? sus) != true)
            {
                return;
            }

            foreach (var (_, uid) in sus.Allies)
            {
                // Otherwise the entity can not exist yet
                if (!_entityManager.TryGetEntity(uid, out var ally))
                {
                    continue;
                }

                if (!ally.TryGetComponent(out IPhysBody? physics))
                {
                    continue;
                }

                if (!ExamineSystemShared.InRangeUnOccluded(ent.Transform.MapPosition, ally.Transform.MapPosition, 15,
                    entity => entity == ent || entity == ally))
                {
                    continue;
                }

                // all entities have a TransformComponent
                var transform = physics.Owner.Transform;

                // if not on the same map, continue
                if (transform.MapID != _eyeManager.CurrentMap || !transform.IsMapTransform)
                {
                    continue;
                }

                var worldBox = physics.GetWorldAABB();

                // if not on screen, or too small, continue
                if (!worldBox.Intersects(in viewport) || worldBox.IsEmpty())
                {
                    continue;
                }

                var screenCoordinates = args.ViewportControl!.WorldToScreen(physics.GetWorldAABB().TopLeft + (0, 0.5f));
                DrawString(args.ScreenHandle, _font, screenCoordinates, _traitorText, Color.OrangeRed);
            }
        }

        private static void DrawString(DrawingHandleScreen handle, Font font, Vector2 pos, string str, Color color)
        {
            var baseLine = new Vector2(pos.X, font.GetAscent(1) + pos.Y);

            foreach (var rune in str.EnumerateRunes())
            {
                var advance = font.DrawChar(handle, rune, baseLine, 1, color);
                baseLine += new Vector2(advance, 0);
            }
        }
    }
}
