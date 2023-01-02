using Content.Shared.Examine;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Client.Suspicion
{
    public sealed class TraitorOverlay : Overlay
    {
        private readonly IEntityManager _entityManager;
        private readonly IPlayerManager _playerManager;
        private readonly EntityLookupSystem _lookup;

        public override OverlaySpace Space => OverlaySpace.ScreenSpace;
        private readonly Font _font;

        private readonly string _traitorText = Loc.GetString("traitor-overlay-traitor-text");

        public TraitorOverlay(
            IEntityManager entityManager,
            IPlayerManager playerManager,
            IResourceCache resourceCache,
            EntityLookupSystem lookup)
        {
            _playerManager = playerManager;
            _entityManager = entityManager;
            _lookup = lookup;

            _font = new VectorFont(resourceCache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            var viewport = args.WorldAABB;

            var ent = _playerManager.LocalPlayer?.ControlledEntity;
            if (_entityManager.TryGetComponent(ent, out SuspicionRoleComponent? sus) != true)
            {
                return;
            }

            foreach (var (_, ally) in sus.Allies)
            {
                // Otherwise the entity can not exist yet
                if (!_entityManager.EntityExists(ally))
                {
                    continue;
                }

                if (!_entityManager.TryGetComponent(ally, out PhysicsComponent? physics))
                {
                    continue;
                }

                var allyXform = _entityManager.GetComponent<TransformComponent>(ally);

                var entPosition = _entityManager.GetComponent<TransformComponent>(ent.Value).MapPosition;
                var allyPosition = allyXform.MapPosition;
                if (!ExamineSystemShared.InRangeUnOccluded(entPosition, allyPosition, 15,
                    entity => entity == ent || entity == ally))
                {
                    continue;
                }

                // if not on the same map, continue
                if (allyXform.MapID != args.Viewport.Eye!.Position.MapId
                    || physics.Owner.IsInContainer())
                {
                    continue;
                }

                var (allyWorldPos, allyWorldRot) = allyXform.GetWorldPositionRotation();

                var worldBox = _lookup.GetWorldAABB(ally, allyXform);

                // if not on screen, or too small, continue
                if (!worldBox.Intersects(in viewport) || worldBox.IsEmpty())
                {
                    continue;
                }

                var screenCoordinates = args.ViewportControl!.WorldToScreen(worldBox.TopLeft + (0, 0.5f));
                args.ScreenHandle.DrawString(_font, screenCoordinates, _traitorText, Color.OrangeRed);
            }
        }
    }
}
