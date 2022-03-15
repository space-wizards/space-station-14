using Content.Shared.SubFloor;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.SubFloor
{
    [UsedImplicitly]
    public class SubFloorShowLayerVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(component.Owner, out SpriteComponent? sprite))
                return;

            if (!component.TryGetData(SubFloorVisuals.SubFloor, out bool subfloor))
                return;

            foreach (var layer in sprite.AllLayers)
            {
                layer.Visible = subfloor;
            }

            if (!sprite.LayerMapTryGet(Layers.FirstLayer, out var firstLayer))
            {
                sprite.Visible = subfloor;
                return;
            }

            // show the top part of the sprite. E.g. the grille-part of a vent, but not the connecting pipes.
            sprite.LayerSetVisible(firstLayer, true);
            sprite.Visible = true;
        }

        public enum Layers : byte
        {
            FirstLayer,
        }
    }
}
