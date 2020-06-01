using Content.Client.GameObjects.Components.IconSmoothing;
using Content.Client.GameObjects.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using static Content.Client.GameObjects.Components.IconSmoothing.IconSmoothComponent;

namespace Content.Client.GameObjects.Components
{
    [RegisterComponent]
    public sealed class WindowComponent : IconSmoothComponent
    {
        public override string Name => "Window";

        public CornerFill LastCornerNE { get; private set; }
        public CornerFill LastCornerSE { get; private set; }
        public CornerFill LastCornerSW { get; private set; }
        public CornerFill LastCornerNW { get; private set; }

        internal override void CalculateNewSprite()
        {
            base.CalculateNewSprite();
            var lowWall = FindLowWall();

            if (lowWall == null)
            {
                var n = MatchingEntity(SnapGrid.GetInDir(Direction.North));
                var ne = MatchingEntity(SnapGrid.GetInDir(Direction.NorthEast));
                var e = MatchingEntity(SnapGrid.GetInDir(Direction.East));
                var se = MatchingEntity(SnapGrid.GetInDir(Direction.SouthEast));
                var s = MatchingEntity(SnapGrid.GetInDir(Direction.South));
                var sw = MatchingEntity(SnapGrid.GetInDir(Direction.SouthWest));
                var w = MatchingEntity(SnapGrid.GetInDir(Direction.West));
                var nw = MatchingEntity(SnapGrid.GetInDir(Direction.NorthWest));

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

                Sprite.LayerSetState(CornerLayers.NE, $"{StateBase}{(int) cornerNE}");
                Sprite.LayerSetState(CornerLayers.SE, $"{StateBase}{(int) cornerSE}");
                Sprite.LayerSetState(CornerLayers.SW, $"{StateBase}{(int) cornerSW}");
                Sprite.LayerSetState(CornerLayers.NW, $"{StateBase}{(int) cornerNW}");

                LastCornerNE = cornerNE;
                LastCornerSE = cornerSE;
                LastCornerSW = cornerSW;
                LastCornerNW = cornerNW;
            }
            else
            {
                Sprite.LayerSetState(CornerLayers.NE, $"{StateBase}_onframe{(int) lowWall.LastCornerNE}");
                Sprite.LayerSetState(CornerLayers.SE, $"{StateBase}_onframe{(int) lowWall.LastCornerSE}");
                Sprite.LayerSetState(CornerLayers.SW, $"{StateBase}_onframe{(int) lowWall.LastCornerSW}");
                Sprite.LayerSetState(CornerLayers.NW, $"{StateBase}_onframe{(int) lowWall.LastCornerNW}");

                LastCornerNE = lowWall.LastCornerNE;
                LastCornerSE = lowWall.LastCornerSE;
                LastCornerSW = lowWall.LastCornerSW;
                LastCornerNW = lowWall.LastCornerNW;
            }
        }

        private LowWallComponent FindLowWall()
        {
            foreach (var entity in SnapGrid.GetLocal())
            {
                if (entity.TryGetComponent(out LowWallComponent lowWall))
                {
                    return lowWall;
                }
            }

            return null;
        }
    }
}
