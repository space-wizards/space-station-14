using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Content.Client.GameObjects.Components.IconSmoothing;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
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

        public CornerFill LastCornerNE { get; private set; }
        public CornerFill LastCornerSE { get; private set; }
        public CornerFill LastCornerSW { get; private set; }
        public CornerFill LastCornerNW { get; private set; }

        [ViewVariables]
        private IEntity _overlayEntity;
        private ISpriteComponent _overlaySprite;

        [ViewVariables(VVAccess.ReadWrite)]
        private Color _overlayColor;

        [ViewVariables(VVAccess.ReadWrite)]
        private Color _edgeColor;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _overlayColor, "overlayColor", Color.White);
            serializer.DataField(ref _edgeColor, "edgeColor", Color.White);
        }

        protected override void Startup()
        {
            base.Startup();

            _overlayEntity = Owner.EntityManager.SpawnEntity("LowWallOverlay", Owner.Transform.GridPosition);
            _overlayEntity.Transform.AttachParent(Owner);

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

            if(_overlayColor != null)
            {
                _overlaySprite.LayerSetColor(OverCornerLayers.SE, _overlayColor);
                _overlaySprite.LayerSetColor(OverCornerLayers.NE, _overlayColor);
                _overlaySprite.LayerSetColor(OverCornerLayers.NW, _overlayColor);
                _overlaySprite.LayerSetColor(OverCornerLayers.SW, _overlayColor);
            }

            var edgeState0 = $"{StateBase}edge_0";
            _overlaySprite.LayerMapSet(BorderCornerLayers.SE, _overlaySprite.AddLayerState(edgeState0));
            _overlaySprite.LayerSetDirOffset(BorderCornerLayers.SE, DirectionOffset.None);
            _overlaySprite.LayerMapSet(BorderCornerLayers.NE, _overlaySprite.AddLayerState(edgeState0));
            _overlaySprite.LayerSetDirOffset(BorderCornerLayers.NE, DirectionOffset.CounterClockwise);
            _overlaySprite.LayerMapSet(BorderCornerLayers.NW, _overlaySprite.AddLayerState(edgeState0));
            _overlaySprite.LayerSetDirOffset(BorderCornerLayers.NW, DirectionOffset.Flip);
            _overlaySprite.LayerMapSet(BorderCornerLayers.SW, _overlaySprite.AddLayerState(edgeState0));
            _overlaySprite.LayerSetDirOffset(BorderCornerLayers.SW, DirectionOffset.Clockwise);

            var otherState0 = $"{StateBase}other_0";
            _overlaySprite.LayerMapSet(OtherCornerLayers.SE, _overlaySprite.AddLayerState(otherState0));
            _overlaySprite.LayerSetDirOffset(OtherCornerLayers.SE, DirectionOffset.None);
            _overlaySprite.LayerMapSet(OtherCornerLayers.NE, _overlaySprite.AddLayerState(otherState0));
            _overlaySprite.LayerSetDirOffset(OtherCornerLayers.NE, DirectionOffset.CounterClockwise);
            _overlaySprite.LayerMapSet(OtherCornerLayers.NW, _overlaySprite.AddLayerState(otherState0));
            _overlaySprite.LayerSetDirOffset(OtherCornerLayers.NW, DirectionOffset.Flip);
            _overlaySprite.LayerMapSet(OtherCornerLayers.SW, _overlaySprite.AddLayerState(otherState0));
            _overlaySprite.LayerSetDirOffset(OtherCornerLayers.SW, DirectionOffset.Clockwise);

            if (_edgeColor != null)
            {
                _overlaySprite.LayerSetColor(BorderCornerLayers.SE, _edgeColor);
                _overlaySprite.LayerSetColor(BorderCornerLayers.NE, _edgeColor);
                _overlaySprite.LayerSetColor(BorderCornerLayers.NW, _edgeColor);
                _overlaySprite.LayerSetColor(BorderCornerLayers.SW, _edgeColor);

                _overlaySprite.LayerSetColor(OtherCornerLayers.SE, _edgeColor);
                _overlaySprite.LayerSetColor(OtherCornerLayers.NE, _edgeColor);
                _overlaySprite.LayerSetColor(OtherCornerLayers.NW, _edgeColor);
                _overlaySprite.LayerSetColor(OtherCornerLayers.SW, _edgeColor);
            }
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            _overlayEntity.Delete();
        }

        internal override void CalculateNewSprite()
        {
            base.CalculateNewSprite();

            var (n, nl, no) = MatchingWall(SnapGrid.GetInDir(Direction.North));
            var (ne, nel, _) = MatchingWall(SnapGrid.GetInDir(Direction.NorthEast));
            var (e, el, eo) = MatchingWall(SnapGrid.GetInDir(Direction.East));
            var (se, sel, _) = MatchingWall(SnapGrid.GetInDir(Direction.SouthEast));
            var (s, sl, so) = MatchingWall(SnapGrid.GetInDir(Direction.South));
            var (sw, swl, _) = MatchingWall(SnapGrid.GetInDir(Direction.SouthWest));
            var (w, wl, wo) = MatchingWall(SnapGrid.GetInDir(Direction.West));
            var (nw, nwl, _) = MatchingWall(SnapGrid.GetInDir(Direction.NorthWest));

            // ReSharper disable InconsistentNaming
            var cornerNE = CornerFill.None;
            var cornerSE = CornerFill.None;
            var cornerSW = CornerFill.None;
            var cornerNW = CornerFill.None;

            var lowCornerNE = CornerFill.None;
            var lowCornerSE = CornerFill.None;
            var lowCornerSW = CornerFill.None;
            var lowCornerNW = CornerFill.None;

            var edgeCornerNE = CornerFill.None;
            var edgeCornerSE = CornerFill.None;
            var edgeCornerSW = CornerFill.None;
            var edgeCornerNW = CornerFill.None;

            var otherCornerNE = CornerFill.None;
            var otherCornerSE = CornerFill.None;
            var otherCornerSW = CornerFill.None;
            var otherCornerNW = CornerFill.None;
            // ReSharper restore InconsistentNaming

            if (n)
            {
                cornerNE |= CornerFill.CounterClockwise;
                cornerNW |= CornerFill.Clockwise;

                if (!nl)
                {
                    lowCornerNE |= CornerFill.CounterClockwise;
                    lowCornerNW |= CornerFill.Clockwise;
                    edgeCornerNE |= CornerFill.CounterClockwise;
                    edgeCornerNW |= CornerFill.Clockwise;
                }

                if (!no)
                {
                    otherCornerNE |= CornerFill.CounterClockwise;
                    otherCornerNW |= CornerFill.Clockwise;
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
                    edgeCornerNE |= CornerFill.Clockwise;
                    edgeCornerSE |= CornerFill.CounterClockwise;
                }

                if(!eo)
                {
                    otherCornerNE |= CornerFill.Clockwise;
                    otherCornerSE |= CornerFill.CounterClockwise;
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
                    edgeCornerSE |= CornerFill.Clockwise;
                    edgeCornerSW |= CornerFill.CounterClockwise;
                }

                if (!so)
                {
                    otherCornerSE |= CornerFill.Clockwise;
                    otherCornerSW |= CornerFill.CounterClockwise;
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
                    edgeCornerSW |= CornerFill.Clockwise;
                    edgeCornerNW |= CornerFill.CounterClockwise;
                }

                if (!wo)
                {
                    otherCornerSW |= CornerFill.Clockwise;
                    otherCornerNW |= CornerFill.CounterClockwise;
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

            _overlaySprite.LayerSetState(BorderCornerLayers.NE, $"{StateBase}edge_{(int) edgeCornerNE}");
            _overlaySprite.LayerSetState(BorderCornerLayers.SE, $"{StateBase}edge_{(int) edgeCornerSE}");
            _overlaySprite.LayerSetState(BorderCornerLayers.SW, $"{StateBase}edge_{(int) edgeCornerSW}");
            _overlaySprite.LayerSetState(BorderCornerLayers.NW, $"{StateBase}edge_{(int) edgeCornerNW}");

            _overlaySprite.LayerSetState(OtherCornerLayers.NE, $"{StateBase}other_{(int) otherCornerNE}");
            _overlaySprite.LayerSetState(OtherCornerLayers.SE, $"{StateBase}other_{(int) otherCornerSE}");
            _overlaySprite.LayerSetState(OtherCornerLayers.SW, $"{StateBase}other_{(int) otherCornerSW}");
            _overlaySprite.LayerSetState(OtherCornerLayers.NW, $"{StateBase}other_{(int) otherCornerNW}");

            LastCornerNE = cornerNE;
            LastCornerSE = cornerSE;
            LastCornerSW = cornerSW;
            LastCornerNW = cornerNW;

            foreach (var entity in SnapGrid.GetLocal())
            {
                if (entity.TryGetComponent(out WindowComponent window))
                {
                    window.CalculateNewSprite();
                }
            }
        }

        [Pure]
        private (bool connected, bool lowWall, bool otherConnection) MatchingWall(IEnumerable<IEntity> candidates)
        {
            foreach (var entity in candidates)
            {
                if (!entity.TryGetComponent(out IconSmoothComponent other))
                {
                    continue;
                }

                if (other.SmoothKey == SmoothKey)
                {
                    if(other is LowWallComponent)
                    {
                        return (true, true, true);
                    }
                    else if(other is WallComponent)
                    {
                        return (true, false, true);
                    }
                    return (true, true, false);
                }
            }

            return (false, false, false);
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum OverCornerLayers
        {
            SE,
            NE,
            NW,
            SW,
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum BorderCornerLayers
        {
            SE,
            NE,
            NW,
            SW,
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum OtherCornerLayers
        {
            SE,
            NE,
            NW,
            SW,
        }
    }
}
