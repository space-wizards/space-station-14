using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Content.Client.GameObjects.Components.IconSmoothing;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client.GameObjects.Components
{
    // TODO: Over layers should be placed ABOVE the window itself too.
    // This is gonna require a client entity & parenting,
    // so IsMapTransform being naive is gonna be a problem.

    /// <summary>
    ///     Override of icon smoothing to handle the specific complexities of low walls.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IconSmoothComponent))]
    public class LowWallComponent : IconSmoothComponent
    {
        public override string Name => "LowWall";

        [Dependency] private readonly IMapManager _mapManager = default!;

        public CornerFill LastCornerNE { get; private set; }
        public CornerFill LastCornerSE { get; private set; }
        public CornerFill LastCornerSW { get; private set; }
        public CornerFill LastCornerNW { get; private set; }

        [ViewVariables] private IEntity? _overlayEntity;

        [ViewVariables]
        private ISpriteComponent? _overlaySprite;

        protected override void Startup()
        {
            base.Startup();

            _overlayEntity = Owner.EntityManager.SpawnEntity("LowWallOverlay", Owner.Transform.Coordinates);
            _overlayEntity.Transform.AttachParent(Owner);
            _overlayEntity.Transform.LocalPosition = Vector2.Zero;

            _overlaySprite = _overlayEntity.GetComponent<ISpriteComponent>();

            var overState0 = $"{StateBase}over_0";
            _overlaySprite.LayerMapSet(OverCornerLayers.SE, _overlaySprite.AddLayerState(overState0));
            _overlaySprite.LayerSetDirOffset(OverCornerLayers.SE, DirectionOffset.None);
            _overlaySprite.LayerMapSet(OverCornerLayers.NE, _overlaySprite.AddLayerState(overState0));
            _overlaySprite.LayerSetDirOffset(OverCornerLayers.NE, DirectionOffset.CounterClockwise);
            _overlaySprite.LayerMapSet(OverCornerLayers.NW, _overlaySprite.AddLayerState(overState0));
            _overlaySprite.LayerSetDirOffset(OverCornerLayers.NW, DirectionOffset.Flip);
            _overlaySprite.LayerMapSet(OverCornerLayers.SW, _overlaySprite.AddLayerState(overState0));
            _overlaySprite.LayerSetDirOffset(OverCornerLayers.SW, DirectionOffset.Clockwise);
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            _overlayEntity?.Delete();
        }

        internal override void CalculateNewSprite()
        {
            base.CalculateNewSprite();

            if (Sprite == null || !Owner.Transform.Anchored || _overlaySprite == null)
            {
                return;
            }

            var grid = _mapManager.GetGrid(Owner.Transform.GridID);
            var coords = Owner.Transform.Coordinates;

            var (n, nl) = MatchingWall(grid.GetInDir(coords, Direction.North));
            var (ne, nel) = MatchingWall(grid.GetInDir(coords, Direction.NorthEast));
            var (e, el) = MatchingWall(grid.GetInDir(coords, Direction.East));
            var (se, sel) = MatchingWall(grid.GetInDir(coords, Direction.SouthEast));
            var (s, sl) = MatchingWall(grid.GetInDir(coords, Direction.South));
            var (sw, swl) = MatchingWall(grid.GetInDir(coords, Direction.SouthWest));
            var (w, wl) = MatchingWall(grid.GetInDir(coords, Direction.West));
            var (nw, nwl) = MatchingWall(grid.GetInDir(coords, Direction.NorthWest));

            // ReSharper disable InconsistentNaming
            var cornerNE = CornerFill.None;
            var cornerSE = CornerFill.None;
            var cornerSW = CornerFill.None;
            var cornerNW = CornerFill.None;

            var lowCornerNE = CornerFill.None;
            var lowCornerSE = CornerFill.None;
            var lowCornerSW = CornerFill.None;
            var lowCornerNW = CornerFill.None;
            // ReSharper restore InconsistentNaming

            if (n)
            {
                cornerNE |= CornerFill.CounterClockwise;
                cornerNW |= CornerFill.Clockwise;

                if (!nl)
                {
                    lowCornerNE |= CornerFill.CounterClockwise;
                    lowCornerNW |= CornerFill.Clockwise;
                }
            }

            if (ne)
            {
                cornerNE |= CornerFill.Diagonal;

                if (!nel && (nl || el || n && e))
                {
                    lowCornerNE |= CornerFill.Diagonal;
                }
            }

            if (e)
            {
                cornerNE |= CornerFill.Clockwise;
                cornerSE |= CornerFill.CounterClockwise;

                if (!el)
                {
                    lowCornerNE |= CornerFill.Clockwise;
                    lowCornerSE |= CornerFill.CounterClockwise;
                }
            }

            if (se)
            {
                cornerSE |= CornerFill.Diagonal;

                if (!sel && (sl || el || s && e))
                {
                    lowCornerSE |= CornerFill.Diagonal;
                }
            }

            if (s)
            {
                cornerSE |= CornerFill.Clockwise;
                cornerSW |= CornerFill.CounterClockwise;

                if (!sl)
                {
                    lowCornerSE |= CornerFill.Clockwise;
                    lowCornerSW |= CornerFill.CounterClockwise;
                }
            }

            if (sw)
            {
                cornerSW |= CornerFill.Diagonal;

                if (!swl && (sl || wl || s && w))
                {
                    lowCornerSW |= CornerFill.Diagonal;
                }
            }

            if (w)
            {
                cornerSW |= CornerFill.Clockwise;
                cornerNW |= CornerFill.CounterClockwise;

                if (!wl)
                {
                    lowCornerSW |= CornerFill.Clockwise;
                    lowCornerNW |= CornerFill.CounterClockwise;
                }
            }

            if (nw)
            {
                cornerNW |= CornerFill.Diagonal;

                if (!nwl && (nl || wl || n && w))
                {
                    lowCornerNW |= CornerFill.Diagonal;
                }
            }

            Sprite.LayerSetState(CornerLayers.NE, $"{StateBase}{(int) cornerNE}");
            Sprite.LayerSetState(CornerLayers.SE, $"{StateBase}{(int) cornerSE}");
            Sprite.LayerSetState(CornerLayers.SW, $"{StateBase}{(int) cornerSW}");
            Sprite.LayerSetState(CornerLayers.NW, $"{StateBase}{(int) cornerNW}");

            _overlaySprite.LayerSetState(OverCornerLayers.NE, $"{StateBase}over_{(int) lowCornerNE}");
            _overlaySprite.LayerSetState(OverCornerLayers.SE, $"{StateBase}over_{(int) lowCornerSE}");
            _overlaySprite.LayerSetState(OverCornerLayers.SW, $"{StateBase}over_{(int) lowCornerSW}");
            _overlaySprite.LayerSetState(OverCornerLayers.NW, $"{StateBase}over_{(int) lowCornerNW}");

            LastCornerNE = cornerNE;
            LastCornerSE = cornerSE;
            LastCornerSW = cornerSW;
            LastCornerNW = cornerNW;

            foreach (var entity in grid.GetLocal(coords))
            {
                if (Owner.EntityManager.ComponentManager.TryGetComponent(entity, out WindowComponent? window))
                {
                    window.UpdateSprite();
                }
            }
        }

        [Pure]
        private (bool connected, bool lowWall) MatchingWall(IEnumerable<EntityUid> candidates)
        {
            foreach (var entity in candidates)
            {
                if (!Owner.EntityManager.ComponentManager.TryGetComponent(entity, out IconSmoothComponent? other))
                {
                    continue;
                }

                if (other.SmoothKey == SmoothKey)
                {
                    return (true, other is LowWallComponent);
                }
            }

            return (false, false);
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum OverCornerLayers : byte
        {
            SE,
            NE,
            NW,
            SW,
        }
    }
}
