using Content.Client.GameObjects.Components.IconSmoothing;
using Content.Server.GameObjects;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IconSmoothComponent))]
    public class WallComponent : IconSmoothComponent
    {
        public override string Name => "Wall";

        [ViewVariables(VVAccess.ReadWrite)]
        private string _reinforcedStateBase;

        [ViewVariables(VVAccess.ReadWrite)]
        private string _paintBase;

        [ViewVariables(VVAccess.ReadWrite)]
        private Color _paintColor;

        [ViewVariables(VVAccess.ReadWrite)]
        private string _stripeBase;

        [ViewVariables(VVAccess.ReadWrite)]
        private Color _stripeColor;

        [ViewVariables(VVAccess.ReadWrite)]
        private string _edgeBase;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _reinforcedStateBase, "reinforcedBase", null);
            serializer.DataField(ref _paintBase, "paintBase", null);
            serializer.DataField(ref _paintColor, "paintColor", Color.White);
            serializer.DataField(ref _stripeBase, "stripeBase", null);
            serializer.DataField(ref _stripeColor, "stripeColor", Color.White);
            serializer.DataField(ref _edgeBase, "edgeBase", null);
        }

        protected override void Startup()
        {
            base.Startup();

            if (_paintBase != null)
            {
                var pstate0 = $"{_paintBase}0";
                Sprite.LayerMapSet(PaintCornerLayers.SE, Sprite.AddLayerState(pstate0));
                Sprite.LayerSetDirOffset(PaintCornerLayers.SE, DirectionOffset.None);
                Sprite.LayerMapSet(PaintCornerLayers.NE, Sprite.AddLayerState(pstate0));
                Sprite.LayerSetDirOffset(PaintCornerLayers.NE, DirectionOffset.CounterClockwise);
                Sprite.LayerMapSet(PaintCornerLayers.NW, Sprite.AddLayerState(pstate0));
                Sprite.LayerSetDirOffset(PaintCornerLayers.NW, DirectionOffset.Flip);
                Sprite.LayerMapSet(PaintCornerLayers.SW, Sprite.AddLayerState(pstate0));
                Sprite.LayerSetDirOffset(PaintCornerLayers.SW, DirectionOffset.Clockwise);

                if(_paintColor != null)
                {
                    Sprite.LayerSetColor(PaintCornerLayers.SE, _paintColor);
                    Sprite.LayerSetColor(PaintCornerLayers.NE, _paintColor);
                    Sprite.LayerSetColor(PaintCornerLayers.NW, _paintColor);
                    Sprite.LayerSetColor(PaintCornerLayers.SW, _paintColor);
                }
            }

            if (_stripeBase != null)
            {
                var sstate0 = $"{_stripeBase}0";
                Sprite.LayerMapSet(StripeCornerLayers.SE, Sprite.AddLayerState(sstate0));
                Sprite.LayerSetDirOffset(StripeCornerLayers.SE, DirectionOffset.None);
                Sprite.LayerMapSet(StripeCornerLayers.NE, Sprite.AddLayerState(sstate0));
                Sprite.LayerSetDirOffset(StripeCornerLayers.NE, DirectionOffset.CounterClockwise);
                Sprite.LayerMapSet(StripeCornerLayers.NW, Sprite.AddLayerState(sstate0));
                Sprite.LayerSetDirOffset(StripeCornerLayers.NW, DirectionOffset.Flip);
                Sprite.LayerMapSet(StripeCornerLayers.SW, Sprite.AddLayerState(sstate0));
                Sprite.LayerSetDirOffset(StripeCornerLayers.SW, DirectionOffset.Clockwise);

                if (_stripeColor != null)
                {
                    Sprite.LayerSetColor(StripeCornerLayers.SE, _stripeColor);
                    Sprite.LayerSetColor(StripeCornerLayers.NE, _stripeColor);
                    Sprite.LayerSetColor(StripeCornerLayers.NW, _stripeColor);
                    Sprite.LayerSetColor(StripeCornerLayers.SW, _stripeColor);
                }
            }

            if (_reinforcedStateBase != null)
            {
                var rstate0 = $"{_reinforcedStateBase}0";
                Sprite.LayerMapSet(ReinforcedCornerLayers.SE, Sprite.AddLayerState(rstate0));
                Sprite.LayerSetDirOffset(ReinforcedCornerLayers.SE, DirectionOffset.None);
                Sprite.LayerMapSet(ReinforcedCornerLayers.NE, Sprite.AddLayerState(rstate0));
                Sprite.LayerSetDirOffset(ReinforcedCornerLayers.NE, DirectionOffset.CounterClockwise);
                Sprite.LayerMapSet(ReinforcedCornerLayers.NW, Sprite.AddLayerState(rstate0));
                Sprite.LayerSetDirOffset(ReinforcedCornerLayers.NW, DirectionOffset.Flip);
                Sprite.LayerMapSet(ReinforcedCornerLayers.SW, Sprite.AddLayerState(rstate0));
                Sprite.LayerSetDirOffset(ReinforcedCornerLayers.SW, DirectionOffset.Clockwise);

                if (_paintColor != null)
                {
                    Sprite.LayerSetColor(ReinforcedCornerLayers.SE, _paintColor);
                    Sprite.LayerSetColor(ReinforcedCornerLayers.NE, _paintColor);
                    Sprite.LayerSetColor(ReinforcedCornerLayers.NW, _paintColor);
                    Sprite.LayerSetColor(ReinforcedCornerLayers.SW, _paintColor);
                }
            }

            if (_edgeBase != null)
            {
                var estate0 = $"{_edgeBase}0";
                Sprite.LayerMapSet(EdgeCornerLayers.SE, Sprite.AddLayerState(estate0));
                Sprite.LayerSetDirOffset(EdgeCornerLayers.SE, DirectionOffset.None);
                Sprite.LayerMapSet(EdgeCornerLayers.NE, Sprite.AddLayerState(estate0));
                Sprite.LayerSetDirOffset(EdgeCornerLayers.NE, DirectionOffset.CounterClockwise);
                Sprite.LayerMapSet(EdgeCornerLayers.NW, Sprite.AddLayerState(estate0));
                Sprite.LayerSetDirOffset(EdgeCornerLayers.NW, DirectionOffset.Flip);
                Sprite.LayerMapSet(EdgeCornerLayers.SW, Sprite.AddLayerState(estate0));
                Sprite.LayerSetDirOffset(EdgeCornerLayers.SW, DirectionOffset.Clockwise);

                if (_stripeColor != null)
                {
                    Sprite.LayerSetColor(EdgeCornerLayers.SE, _stripeColor);
                    Sprite.LayerSetColor(EdgeCornerLayers.NE, _stripeColor);
                    Sprite.LayerSetColor(EdgeCornerLayers.NW, _stripeColor);
                    Sprite.LayerSetColor(EdgeCornerLayers.SW, _stripeColor);
                }
            }
        }

        internal override void CalculateNewSprite()
        {
            base.CalculateNewSprite();

            var (cornerNE, cornerNW, cornerSW, cornerSE) = CalculateCornerFill();

            if (_paintBase != null)
            {
                Sprite.LayerSetState(PaintCornerLayers.NE, $"{_paintBase}{(int) cornerNE}");
                Sprite.LayerSetState(PaintCornerLayers.SE, $"{_paintBase}{(int) cornerSE}");
                Sprite.LayerSetState(PaintCornerLayers.SW, $"{_paintBase}{(int) cornerSW}");
                Sprite.LayerSetState(PaintCornerLayers.NW, $"{_paintBase}{(int) cornerNW}");
            }

            if (_stripeBase != null)
            {
                Sprite.LayerSetState(StripeCornerLayers.NE, $"{_stripeBase}{(int) cornerNE}");
                Sprite.LayerSetState(StripeCornerLayers.SE, $"{_stripeBase}{(int) cornerSE}");
                Sprite.LayerSetState(StripeCornerLayers.SW, $"{_stripeBase}{(int) cornerSW}");
                Sprite.LayerSetState(StripeCornerLayers.NW, $"{_stripeBase}{(int) cornerNW}");
            }

            if (_reinforcedStateBase != null)
            {
                Sprite.LayerSetState(ReinforcedCornerLayers.NE, $"{_reinforcedStateBase}{(int) cornerNE}");
                Sprite.LayerSetState(ReinforcedCornerLayers.SE, $"{_reinforcedStateBase}{(int) cornerSE}");
                Sprite.LayerSetState(ReinforcedCornerLayers.SW, $"{_reinforcedStateBase}{(int) cornerSW}");
                Sprite.LayerSetState(ReinforcedCornerLayers.NW, $"{_reinforcedStateBase}{(int) cornerNW}");
            }

            if (_edgeBase != null)
            {
                var (n, nl) = MatchingWall(SnapGrid.GetInDir(Direction.North));
                var (e, el) = MatchingWall(SnapGrid.GetInDir(Direction.East));
                var (s, sl) = MatchingWall(SnapGrid.GetInDir(Direction.South));
                var (w, wl) = MatchingWall(SnapGrid.GetInDir(Direction.West));

                // ReSharper disable InconsistentNaming
                var edgeCornerNE = CornerFill.None;
                var edgeCornerSE = CornerFill.None;
                var edgeCornerSW = CornerFill.None;
                var edgeCornerNW = CornerFill.None;
                // ReSharper restore InconsistentNaming

                if (n && !nl)
                {
                    edgeCornerNE |= CornerFill.CounterClockwise;
                    edgeCornerNW |= CornerFill.Clockwise;
                }

                if (e && !el)
                {
                    edgeCornerNE |= CornerFill.Clockwise;
                    edgeCornerSE |= CornerFill.CounterClockwise;
                }

                if (s && !sl)
                {
                    edgeCornerSE |= CornerFill.Clockwise;
                    edgeCornerSW |= CornerFill.CounterClockwise;
                }

                if (w && !wl)
                {
                    edgeCornerSW |= CornerFill.Clockwise;
                    edgeCornerNW |= CornerFill.CounterClockwise;
                }

                Sprite.LayerSetState(EdgeCornerLayers.NE, $"{_edgeBase}{(int) edgeCornerNE}");
                Sprite.LayerSetState(EdgeCornerLayers.SE, $"{_edgeBase}{(int) edgeCornerSE}");
                Sprite.LayerSetState(EdgeCornerLayers.SW, $"{_edgeBase}{(int) edgeCornerSW}");
                Sprite.LayerSetState(EdgeCornerLayers.NW, $"{_edgeBase}{(int) edgeCornerNW}");
            }
        }

        [Pure]
        private (bool connected, bool lowWall) MatchingWall(IEnumerable<IEntity> candidates)
        {
            foreach (var entity in candidates)
            {
                if (!entity.TryGetComponent(out IconSmoothComponent otherSmooth))
                {
                    continue;
                }

                if (otherSmooth.SmoothKey == SmoothKey)
                {
                    return (true, (otherSmooth is WallComponent || otherSmooth is LowWallComponent));
                }
            }

            return (false, false);
        }

        public enum PaintCornerLayers
        {
            SE,
            NE,
            NW,
            SW,
        }

        public enum StripeCornerLayers
        {
            SE,
            NE,
            NW,
            SW,
        }

        public enum ReinforcedCornerLayers
        {
            SE,
            NE,
            NW,
            SW,
        }

        public enum EdgeCornerLayers
        {
            SE,
            NE,
            NW,
            SW,
        }
    }
}
