using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client.IconSmoothing
{
    /// <summary>
    ///     Entity system implementing the logic for <see cref="IconSmoothComponent"/>
    /// </summary>
    [UsedImplicitly]
    internal sealed class IconSmoothSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        private readonly Queue<EntityUid> _dirtyEntities = new();

        private int _generation;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<IconSmoothComponent, AnchorStateChangedEvent>(HandleAnchorChanged);
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            if (_dirtyEntities.Count == 0)
            {
                return;
            }

            _generation += 1;

            // Performance: This could be spread over multiple updates, or made parallel.
            while (_dirtyEntities.Count > 0)
            {
                CalculateNewSprite(_dirtyEntities.Dequeue());
            }
        }

        public void UpdateSmoothing(EntityUid uid, IconSmoothComponent? comp = null)
        {
            if (!Resolve(uid, ref comp))
                return;

            _dirtyEntities.Enqueue(uid);

            var transform = Transform(uid);
            Vector2i pos;

            if (transform.Anchored && _mapManager.TryGetGrid(transform.GridID, out var grid))
            {
                pos = grid.CoordinatesToTile(transform.Coordinates);
            }
            else
            {
                // Entity is no longer valid, update around the last position it was at.
                if (comp.LastPosition is not (GridId gridId, Vector2i oldPos))
                    return;

                if (!_mapManager.TryGetGrid(gridId, out grid))
                    return;

                pos = oldPos;
            }

            // Yes, we updates ALL smoothing entities surrounding us even if they would never smooth with us.
            // This is simpler to implement. If you want to optimize it be my guest.

            AddValidEntities(grid.GetAnchoredEntities(pos + new Vector2i(1, 0)));
            AddValidEntities(grid.GetAnchoredEntities(pos + new Vector2i(-1, 0)));
            AddValidEntities(grid.GetAnchoredEntities(pos + new Vector2i(0, 1)));
            AddValidEntities(grid.GetAnchoredEntities(pos + new Vector2i(0, -1)));
            if (comp.Mode == IconSmoothingMode.Corners)
            {
                AddValidEntities(grid.GetAnchoredEntities(pos + new Vector2i(1, 1)));
                AddValidEntities(grid.GetAnchoredEntities(pos + new Vector2i(-1, -1)));
                AddValidEntities(grid.GetAnchoredEntities(pos + new Vector2i(-1, 1)));
                AddValidEntities(grid.GetAnchoredEntities(pos + new Vector2i(1, -1)));
            }
        }

        private void HandleAnchorChanged(EntityUid uid, IconSmoothComponent component, ref AnchorStateChangedEvent args)
        {
            UpdateSmoothing(uid, component);
        }

        private void AddValidEntities(IEnumerable<EntityUid> candidates)
        {
            foreach (var entity in candidates)
            {
                if (EntityManager.HasComponent<IconSmoothComponent>(entity))
                {
                    _dirtyEntities.Enqueue(entity);
                }
            }
        }

        private void CalculateNewSprite(EntityUid euid)
        {
            // The generation check prevents updating an entity multiple times per tick.
            // As it stands now, it's totally possible for something to get queued twice.
            // Generation on the component is set after an update so we can cull updates that happened this generation.
            if (!EntityManager.EntityExists(euid)
                || !EntityManager.TryGetComponent(euid, out IconSmoothComponent? smoothing)
                || smoothing.UpdateGeneration == _generation)
            {
                return;
            }

            smoothing.CalculateNewSprite();

            smoothing.UpdateGeneration = _generation;
        }
    }
}
