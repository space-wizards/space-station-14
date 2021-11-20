using Content.Shared.SubFloor;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.SubFloor
{
    [UsedImplicitly]
    public class SubFloorShowLayerVisualizer : AppearanceVisualizer
    {
        /// <summary>
        ///     Set this to any positive value. It will be subtracted from
        ///     the actual drawdepth of the sprite when the visualizer is
        ///     set to show subfloor, and added when it is set to not
        ///     show subfloor.
        /// </summary>
        [DataField("rank")]
        private uint _drawDepth { get; set; } = 0;

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out SpriteComponent? sprite))
                return;

            if (component.TryGetData(SubFloorVisuals.SubFloor, out bool subfloor))
            {
                sprite.Visible = true;

                // Due to the way this visualizer works, you might want to specify it before any other
                // visualizer that hides/shows layers depending on certain conditions, such as PipeConnectorVisualizer.
                foreach (var layer in sprite.AllLayers)
                {
                    layer.Visible = subfloor;
                }

                if (sprite.LayerMapTryGet(Layers.FirstLayer, out var firstLayer))
                {
                    sprite.LayerSetVisible(firstLayer, true);
                }

                if (component.Owner.TryGetComponent(out SubFloorHideComponent? hide))
                {
                    switch (hide.DepthToggle)
                    {
                        case SubFloorVisuals.ToggleDepthOn:
                            sprite.DrawDepth = (int) (sprite.DrawDepth - _drawDepth);
                            Logger.DebugS("SubFloorVisualizer", $"Set DrawDepth to {sprite.DrawDepth}");
                            hide.DepthToggle = SubFloorVisuals.ToggleDepthIgnore;
                            break;
                        case SubFloorVisuals.ToggleDepthOff:
                            sprite.DrawDepth = (int) (sprite.DrawDepth + _drawDepth);
                            Logger.DebugS("SubFloorVisualizer", $"Set DrawDepth to {sprite.DrawDepth}");
                            hide.DepthToggle = SubFloorVisuals.ToggleDepthIgnore;
                            break;
                    }
                }
            }
        }

        public enum Layers : byte
        {
            FirstLayer,
        }
    }
}
