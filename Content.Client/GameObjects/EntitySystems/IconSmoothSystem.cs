using System.Collections.Generic;
using System.Linq;
using Content.Client.GameObjects.Components.IconSmoothing;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.EntitySystems
{
    /// <summary>
    ///     Entity system implementing the logic for <see cref="IconSmoothComponent"/>
    /// </summary>
    [UsedImplicitly]
    internal sealed class IconSmoothSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        private readonly Queue<IEntity> _dirtyEntities = new();

        private int _generation;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<IconSmoothDirtyEvent>(HandleDirtyEvent);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<IconSmoothDirtyEvent>();
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
                var snapGrid = senderEnt.GetComponent<SnapGridComponent>();

                _dirtyEntities.Enqueue(senderEnt);
                AddValidEntities(snapGrid.GetInDir(Direction.North));
                AddValidEntities(snapGrid.GetInDir(Direction.South));
                AddValidEntities(snapGrid.GetInDir(Direction.East));
                AddValidEntities(snapGrid.GetInDir(Direction.West));
                if (ev.Mode == IconSmoothingMode.Corners)
                {

                    AddValidEntities(snapGrid.GetInDir(Direction.NorthEast));
                    AddValidEntities(snapGrid.GetInDir(Direction.SouthEast));
                    AddValidEntities(snapGrid.GetInDir(Direction.SouthWest));
                    AddValidEntities(snapGrid.GetInDir(Direction.NorthWest));
                }
            }

            // Entity is no longer valid, update around the last position it was at.
            if (ev.LastPosition.HasValue && _mapManager.TryGetGrid(ev.LastPosition.Value.grid, out var grid))
            {
                var pos = ev.LastPosition.Value.pos;

                AddValidEntities(grid.GetSnapGridCell(pos + new Vector2i(1, 0), ev.Offset));
                AddValidEntities(grid.GetSnapGridCell(pos + new Vector2i(-1, 0), ev.Offset));
                AddValidEntities(grid.GetSnapGridCell(pos + new Vector2i(0, 1), ev.Offset));
                AddValidEntities(grid.GetSnapGridCell(pos + new Vector2i(0, -1), ev.Offset));
                if (ev.Mode == IconSmoothingMode.Corners)
                {
                    AddValidEntities(grid.GetSnapGridCell(pos + new Vector2i(1, 1), ev.Offset));
                    AddValidEntities(grid.GetSnapGridCell(pos + new Vector2i(-1, -1), ev.Offset));
                    AddValidEntities(grid.GetSnapGridCell(pos + new Vector2i(-1, 1), ev.Offset));
                    AddValidEntities(grid.GetSnapGridCell(pos + new Vector2i(1, -1), ev.Offset));
                }
            }
        }

        private void AddValidEntities(IEnumerable<IEntity> candidates)
        {
            foreach (var entity in candidates)
            {
                if (entity.HasComponent<IconSmoothComponent>())
                {
                    _dirtyEntities.Enqueue(entity);
                }
            }
        }

        private void AddValidEntities(IEnumerable<IComponent> candidates)
        {
            AddValidEntities(candidates.Select(c => c.Owner));
        }

        private void CalculateNewSprite(IEntity entity)
        {
            // The generation check prevents updating an entity multiple times per tick.
            // As it stands now, it's totally possible for something to get queued twice.
            // Generation on the component is set after an update so we can cull updates that happened this generation.
            if (!entity.IsValid()
                || !entity.TryGetComponent(out IconSmoothComponent? smoothing)
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
        public IconSmoothDirtyEvent(IEntity sender, (GridId grid, Vector2i pos)? lastPosition, SnapGridOffset offset, IconSmoothingMode mode)
        {
            LastPosition = lastPosition;
            Offset = offset;
            Mode = mode;
            Sender = sender;
        }

        public (GridId grid, Vector2i pos)? LastPosition { get; }
        public SnapGridOffset Offset { get; }
        public IconSmoothingMode Mode { get; }
        public IEntity Sender { get; }
    }
}
