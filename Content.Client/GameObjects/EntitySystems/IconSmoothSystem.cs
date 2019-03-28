using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.GameObjects.Components.IconSmoothing;
using JetBrains.Annotations;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Transform;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace Content.Client.GameObjects.EntitySystems
{
    /// <summary>
    ///     Entity system implementing the logic for <see cref="IconSmoothComponent"/>
    /// </summary>
    [UsedImplicitly]
    internal sealed class IconSmoothSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        private readonly Queue<IEntity> _dirtyEntities = new Queue<IEntity>();

        private int _generation;

        public override void SubscribeEvents()
        {
            base.SubscribeEvents();

            SubscribeEvent<IconSmoothDirtyEvent>(HandleDirtyEvent);
        }

        public override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);
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

        private void HandleDirtyEvent(object sender, IconSmoothDirtyEvent ev)
        {
            // Yes, we updates ALL smoothing entities surrounding us even if they would never smooth with us.
            // This is simpler to implement. If you want to optimize it be my guest.
            if (sender is IEntity senderEnt && senderEnt.IsValid() &&
                senderEnt.HasComponent<IconSmoothComponent>())
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
            else if (ev.LastPosition.HasValue)
            {
                // Entity is no longer valid, update around the last position it was at.
                var grid = _mapManager.GetGrid(ev.LastPosition.Value.grid);
                var pos = ev.LastPosition.Value.pos;

                AddValidEntities(grid.GetSnapGridCell(pos + new MapIndices(1, 0), ev.Offset));
                AddValidEntities(grid.GetSnapGridCell(pos + new MapIndices(-1, 0), ev.Offset));
                AddValidEntities(grid.GetSnapGridCell(pos + new MapIndices(0, 1), ev.Offset));
                AddValidEntities(grid.GetSnapGridCell(pos + new MapIndices(0, -1), ev.Offset));
                if (ev.Mode == IconSmoothingMode.Corners)
                {
                    AddValidEntities(grid.GetSnapGridCell(pos + new MapIndices(1, 1), ev.Offset));
                    AddValidEntities(grid.GetSnapGridCell(pos + new MapIndices(-1, -1), ev.Offset));
                    AddValidEntities(grid.GetSnapGridCell(pos + new MapIndices(-1, 1), ev.Offset));
                    AddValidEntities(grid.GetSnapGridCell(pos + new MapIndices(1, -1), ev.Offset));
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
                || !entity.TryGetComponent(out IconSmoothComponent smoothing)
                || smoothing.UpdateGeneration == _generation)
            {
                return;
            }

            var sprite = smoothing.Sprite;
            var snapGrid = smoothing.SnapGrid;

            switch (smoothing.Mode)
            {
                case IconSmoothingMode.Corners:
                    _calculateNewSpriteCorers(smoothing, snapGrid, sprite);
                    break;

                case IconSmoothingMode.CardinalFlags:
                    _calculateNewSpriteCardinal(smoothing, snapGrid, sprite);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            smoothing.UpdateGeneration = _generation;
        }

        private static void _calculateNewSpriteCardinal(IconSmoothComponent smoothing, SnapGridComponent snapGrid,
            ISpriteComponent sprite)
        {
            var dirs = CardinalConnectDirs.None;

            if (MatchingEntity(smoothing, snapGrid.GetInDir(Direction.North)))
                dirs |= CardinalConnectDirs.North;
            if (MatchingEntity(smoothing, snapGrid.GetInDir(Direction.South)))
                dirs |= CardinalConnectDirs.South;
            if (MatchingEntity(smoothing, snapGrid.GetInDir(Direction.East)))
                dirs |= CardinalConnectDirs.East;
            if (MatchingEntity(smoothing, snapGrid.GetInDir(Direction.West)))
                dirs |= CardinalConnectDirs.West;

            sprite.LayerSetState(0, $"{smoothing.StateBase}{(int) dirs}");
        }

        private static void _calculateNewSpriteCorers(IconSmoothComponent smoothing, SnapGridComponent snapGrid,
            ISpriteComponent sprite)
        {
            var n = MatchingEntity(smoothing, snapGrid.GetInDir(Direction.North));
            var ne = MatchingEntity(smoothing, snapGrid.GetInDir(Direction.NorthEast));
            var e = MatchingEntity(smoothing, snapGrid.GetInDir(Direction.East));
            var se = MatchingEntity(smoothing, snapGrid.GetInDir(Direction.SouthEast));
            var s = MatchingEntity(smoothing, snapGrid.GetInDir(Direction.South));
            var sw = MatchingEntity(smoothing, snapGrid.GetInDir(Direction.SouthWest));
            var w = MatchingEntity(smoothing, snapGrid.GetInDir(Direction.West));
            var nw = MatchingEntity(smoothing, snapGrid.GetInDir(Direction.NorthWest));

            // ReSharper disable InconsistentNaming
            var cornerNE = CornerFill.None;
            var cornerSE = CornerFill.None;
            var cornerSW = CornerFill.None;
            var cornerNW = CornerFill.None;
            // ReSharper restore InconsistentNaming

            if (n)
            {
                cornerNE |= CornerFill.CounterClockwise;
                cornerNW |= CornerFill.Clockwise;
            }

            if (ne)
            {
                cornerNE |= CornerFill.Diagonal;
            }

            if (e)
            {
                cornerNE |= CornerFill.Clockwise;
                cornerSE |= CornerFill.CounterClockwise;
            }

            if (se)
            {
                cornerSE |= CornerFill.Diagonal;
            }

            if (s)
            {
                cornerSE |= CornerFill.Clockwise;
                cornerSW |= CornerFill.CounterClockwise;
            }

            if (sw)
            {
                cornerSW |= CornerFill.Diagonal;
            }

            if (w)
            {
                cornerSW |= CornerFill.Clockwise;
                cornerNW |= CornerFill.CounterClockwise;
            }

            if (nw)
            {
                cornerNW |= CornerFill.Diagonal;
            }

            sprite.LayerSetState(IconSmoothComponent.CornerLayers.NE, $"{smoothing.StateBase}{(int) cornerNE}");
            sprite.LayerSetState(IconSmoothComponent.CornerLayers.SE, $"{smoothing.StateBase}{(int) cornerSE}");
            sprite.LayerSetState(IconSmoothComponent.CornerLayers.SW, $"{smoothing.StateBase}{(int) cornerSW}");
            sprite.LayerSetState(IconSmoothComponent.CornerLayers.NW, $"{smoothing.StateBase}{(int) cornerNW}");
        }

        [System.Diagnostics.Contracts.Pure]
        private static bool MatchingEntity(IconSmoothComponent source, IEnumerable<IEntity> candidates)
        {
            foreach (var entity in candidates)
            {
                if (!entity.TryGetComponent(out IconSmoothComponent other))
                {
                    return false;
                }

                if (other.SmoothKey == source.SmoothKey)
                {
                    return true;
                }
            }

            return false;
        }

        [Flags]
        private enum CardinalConnectDirs : byte
        {
            None = 0,
            North = 1,
            South = 2,
            East = 4,
            West = 8
        }

        [Flags]
        private enum CornerFill : byte
        {
            // These values are pulled from Baystation12.
            // I'm too lazy to convert the state names.
            None = 0,

            // The cardinal tile counter-clockwise of this corner is filled.
            CounterClockwise = 1,

            // The diagonal tile in the direction of this corner.
            Diagonal = 2,

            // The cardinal tile clockwise of this corner is filled.
            Clockwise = 4,
        }
    }

    /// <summary>
    ///     Event raised by a <see cref="IconSmoothComponent"/> when it needs to be recalculated.
    /// </summary>
    public sealed class IconSmoothDirtyEvent : EntitySystemMessage
    {
        public IconSmoothDirtyEvent((GridId grid, MapIndices pos)? lastPosition, SnapGridOffset offset, IconSmoothingMode mode)
        {
            LastPosition = lastPosition;
            Offset = offset;
            Mode = mode;
        }

        public (GridId grid, MapIndices pos)? LastPosition { get; }
        public SnapGridOffset Offset { get; }
        public IconSmoothingMode Mode { get; }
    }
}
