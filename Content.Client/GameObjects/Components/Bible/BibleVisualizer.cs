using Content.Shared.GameObjects.Components.Bible;
using Robust.Client.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Client.GameObjects.Components.Bible
{
    public class BibleVisualizer : AppearanceVisualizer
    {
        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (entity.TryGetComponent(out SpriteComponent sprite))
                sprite.LayerMapReserveBlank(BibleVisualLayers.Base);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            if (!component.Owner.TryGetComponent(out SpriteComponent sprite))
                return;

            if (component.TryGetData(BibleVisuals.Style, out string style))
            {
                var layer = sprite.LayerMapGet(BibleVisualLayers.Base);
                sprite.LayerSetState(layer, style);
            }
        }
    }

    public enum BibleVisualLayers
    {
        Base
    }
}
