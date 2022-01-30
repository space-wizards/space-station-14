using Content.Client.IconSmoothing;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client.Wall.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IconSmoothComponent))]
    public class ReinforcedWallComponent : IconSmoothComponent // whyyyyyyyyy
    {
        public override string Name => "ReinforcedWall";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("reinforcedBase")]
        private string? _reinforcedStateBase = default;

        protected override void Startup()
        {
            base.Startup();

            if (Sprite != null)
            {
                var state0 = $"{_reinforcedStateBase}0";
                Sprite.LayerMapSet(ReinforcedCornerLayers.SE, Sprite.AddLayerState(state0));
                Sprite.LayerSetDirOffset(ReinforcedCornerLayers.SE, DirectionOffset.None);
                Sprite.LayerMapSet(ReinforcedCornerLayers.NE, Sprite.AddLayerState(state0));
                Sprite.LayerSetDirOffset(ReinforcedCornerLayers.NE, DirectionOffset.CounterClockwise);
                Sprite.LayerMapSet(ReinforcedCornerLayers.NW, Sprite.AddLayerState(state0));
                Sprite.LayerSetDirOffset(ReinforcedCornerLayers.NW, DirectionOffset.Flip);
                Sprite.LayerMapSet(ReinforcedCornerLayers.SW, Sprite.AddLayerState(state0));
                Sprite.LayerSetDirOffset(ReinforcedCornerLayers.SW, DirectionOffset.Clockwise);
                Sprite.LayerMapSet(ReinforcedWallVisualLayers.Deconstruction, Sprite.AddBlankLayer());
            }
        }

        internal override void CalculateNewSprite(IMapGrid? grid)
        {
            base.CalculateNewSprite(grid);

            var (cornerNE, cornerNW, cornerSW, cornerSE) = CalculateCornerFill(grid);

            if (Sprite != null)
            {
                Sprite.LayerSetState(ReinforcedCornerLayers.NE, $"{_reinforcedStateBase}{(int) cornerNE}");
                Sprite.LayerSetState(ReinforcedCornerLayers.SE, $"{_reinforcedStateBase}{(int) cornerSE}");
                Sprite.LayerSetState(ReinforcedCornerLayers.SW, $"{_reinforcedStateBase}{(int) cornerSW}");
                Sprite.LayerSetState(ReinforcedCornerLayers.NW, $"{_reinforcedStateBase}{(int) cornerNW}");
            }
        }

        public enum ReinforcedCornerLayers : byte
        {
            SE,
            NE,
            NW,
            SW,
        }
    }
}
