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

        [ViewVariables]
        public CornerFill LastWallCornerNE { get; private set; }
        [ViewVariables]
        public CornerFill LastWallCornerSE { get; private set; }
        [ViewVariables]
        public CornerFill LastWallCornerSW { get; private set; }
        [ViewVariables]
        public CornerFill LastWallCornerNW { get; private set; }

        [ViewVariables]
        public CornerFill LastOverlayCornerNE { get; private set; }
        [ViewVariables]
        public CornerFill LastOverlayCornerSE { get; private set; }
        [ViewVariables]
        public CornerFill LastOverlayCornerSW { get; private set; }
        [ViewVariables]
        public CornerFill LastOverlayCornerNW { get; private set; }

        internal override void CalculateNewSprite()
        {
            base.CalculateNewSprite();

            if (Sprite == null || !Owner.Transform.Anchored)
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
            var wallCornerNE = CornerFill.None;
            var wallCornerSE = CornerFill.None;
            var wallCornerSW = CornerFill.None;
            var wallCornerNW = CornerFill.None;

            var overlayCornerNE = CornerFill.None;
            var overlayCornerSE = CornerFill.None;
            var overlayCornerSW = CornerFill.None;
            var overlayCornerNW = CornerFill.None;
            // ReSharper restore InconsistentNaming

            if (n)
            {
                wallCornerNE |= CornerFill.CounterClockwise;
                wallCornerNW |= CornerFill.Clockwise;

                if (!nl && !e && !w)
                {
                    overlayCornerNE |= CornerFill.CounterClockwise;
                    overlayCornerNW |= CornerFill.Clockwise;
                }
            }

            if (ne)
            {
                wallCornerNE |= CornerFill.Diagonal;

                if (!nel && (nl || el || n && e))
                {
                    overlayCornerNE |= CornerFill.Diagonal;
                }
            }

            if (e)
            {
                wallCornerNE |= CornerFill.Clockwise;
                wallCornerSE |= CornerFill.CounterClockwise;

                if (!el)
                {
                    overlayCornerNE |= CornerFill.Clockwise;
                    overlayCornerSE |= CornerFill.CounterClockwise;
                }
            }

            if (se)
            {
                wallCornerSE |= CornerFill.Diagonal;

                if (!sel && (sl || el || s && e))
                {
                    overlayCornerSE |= CornerFill.Diagonal;
                }
            }

            if (s)
            {
                wallCornerSE |= CornerFill.Clockwise;
                wallCornerSW |= CornerFill.CounterClockwise;

                if (!sl)
                {
                    overlayCornerSE |= CornerFill.Clockwise;
                    overlayCornerSW |= CornerFill.CounterClockwise;
                }
            }

            if (sw)
            {
                wallCornerSW |= CornerFill.Diagonal;

                if (!swl && (sl || wl || s && w))
                {
                    overlayCornerSW |= CornerFill.Diagonal;
                }
            }

            if (w)
            {
                wallCornerSW |= CornerFill.Clockwise;
                wallCornerNW |= CornerFill.CounterClockwise;

                if (!wl)
                {
                    overlayCornerSW |= CornerFill.Clockwise;
                    overlayCornerNW |= CornerFill.CounterClockwise;
                }
            }

            if (nw)
            {
                wallCornerNW |= CornerFill.Diagonal;

                if (!nwl && (nl || wl || n && w))
                {
                    overlayCornerNW |= CornerFill.Diagonal;
                }
            }

            Sprite.LayerSetState(CornerLayers.NE, $"{StateBase}{(int) wallCornerNE}");
            Sprite.LayerSetState(CornerLayers.SE, $"{StateBase}{(int) wallCornerSE}");
            Sprite.LayerSetState(CornerLayers.SW, $"{StateBase}{(int) wallCornerSW}");
            Sprite.LayerSetState(CornerLayers.NW, $"{StateBase}{(int) wallCornerNW}");

            LastWallCornerNE = wallCornerNE;
            LastWallCornerSE = wallCornerSE;
            LastWallCornerSW = wallCornerSW;
            LastWallCornerNW = wallCornerNW;

            LastOverlayCornerNE = overlayCornerNE;
            LastOverlayCornerSE = overlayCornerSE;
            LastOverlayCornerSW = overlayCornerSW;
            LastOverlayCornerNW = overlayCornerNW;

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
    }
}
