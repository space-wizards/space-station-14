using System.Collections.Generic;
using System.Linq;
using Content.Client.GameObjects.Components.IconSmoothing;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.GameObjects.EntitySystems
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

            SubscribeLocalEvent<IconSmoothDirtyEvent>(HandleDirtyEvent);

            SubscribeLocalEvent<IconSmoothComponent, SnapGridPositionChangedEvent>(HandleSnapGridMove);
        }


        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<IconSmoothDirtyEvent>();

            UnsubscribeLocalEvent<IconSmoothComponent, SnapGridPositionChangedEvent>(HandleSnapGridMove);
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

        private void HandleDirtyEvent(IconSmoothDirtyEvent ev)
        {
            // Yes, we updates ALL smoothing entities surrounding us even if they would never smooth with us.
            // This is simpler to implement. If you want to optimize it be my guest.
            var senderEnt = ev.Sender;
            if (senderEnt.IsValid() &&
                senderEnt.TryGetComponent(out IconSmoothComponent? iconSmooth)
                && iconSmooth.Running)
            {
                var grid1 = _mapManager.GetGrid(senderEnt.Transform.GridID);
                var coords = senderEnt.Transform.Coordinates;

                _dirtyEntities.Enqueue(senderEnt.Uid);
                AddValidEntities(grid1.GetInDir(coords, Direction.North));
                AddValidEntities(grid1.GetInDir(coords, Direction.South));
                AddValidEntities(grid1.GetInDir(coords, Direction.East));
                AddValidEntities(grid1.GetInDir(coords, Direction.West));
                if (ev.Mode == IconSmoothingMode.Corners)
                {
                    AddValidEntities(grid1.GetInDir(coords, Direction.NorthEast));
                    AddValidEntities(grid1.GetInDir(coords, Direction.SouthEast));
                    AddValidEntities(grid1.GetInDir(coords, Direction.SouthWest));
                    AddValidEntities(grid1.GetInDir(coords, Direction.NorthWest));
                }
            }

            // Entity is no longer valid, update around the last position it was at.
            if (ev.LastPosition.HasValue && _mapManager.TryGetGrid(ev.LastPosition.Value.grid, out var grid))
            {
                var pos = ev.LastPosition.Value.pos;

                AddValidEntities(grid.GetAnchoredEntities(pos + new Vector2i(1, 0)));
                AddValidEntities(grid.GetAnchoredEntities(pos + new Vector2i(-1, 0)));
                AddValidEntities(grid.GetAnchoredEntities(pos + new Vector2i(0, 1)));
                AddValidEntities(grid.GetAnchoredEntities(pos + new Vector2i(0, -1)));
                if (ev.Mode == IconSmoothingMode.Corners)
                {
                    AddValidEntities(grid.GetAnchoredEntities(pos + new Vector2i(1, 1)));
                    AddValidEntities(grid.GetAnchoredEntities(pos + new Vector2i(-1, -1)));
                    AddValidEntities(grid.GetAnchoredEntities(pos + new Vector2i(-1, 1)));
                    AddValidEntities(grid.GetAnchoredEntities(pos + new Vector2i(1, -1)));
                }
            }
        }

        private static void HandleSnapGridMove(EntityUid uid, IconSmoothComponent component, SnapGridPositionChangedEvent args)
        {
            component.SnapGridOnPositionChanged();
        }

        private void AddValidEntities(IEnumerable<EntityUid> candidates)
        {
            foreach (var entity in candidates)
            {
                if (ComponentManager.HasComponent<IconSmoothComponent>(entity))
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
                || !ComponentManager.TryGetComponent(euid, out IconSmoothComponent? smoothing)
                || smoothing.UpdateGeneration == _generation)
            {
                return;
            }

            smoothing.CalculateNewSprite();

            smoothing.UpdateGeneration = _generation;
        }
    }

    /// <summary>
    ///     Event raised by a <see cref="IconSmoothComponent"/> when it needs to be recalculated.
    /// </summary>
    public sealed class IconSmoothDirtyEvent : EntityEventArgs
    {
        public IconSmoothDirtyEvent(IEntity sender, (GridId grid, Vector2i pos)? lastPosition, IconSmoothingMode mode)
        {
            LastPosition = lastPosition;
            Mode = mode;
            Sender = sender;
        }

        public (GridId grid, Vector2i pos)? LastPosition { get; }
        public IconSmoothingMode Mode { get; }
        public IEntity Sender { get; }
    }
}
