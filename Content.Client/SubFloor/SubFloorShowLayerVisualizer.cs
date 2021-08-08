using Content.Shared.SubFloor;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.SubFloor
{
    [UsedImplicitly]
    public class SubFloorShowLayerVisualizer : AppearanceVisualizer
    {
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
            }
        }

        public enum Layers : byte
        {
            FirstLayer,
        }
    }
}
