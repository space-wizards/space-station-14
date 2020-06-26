using Content.Shared.GameObjects.Components.Power;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Client.GameObjects.Storage
{
    [UsedImplicitly]
    public class PowerChargerVisualizer2D : AppearanceVisualizer
    {
        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            var sprite = entity.GetComponent<ISpriteComponent>();

            // Base item
            sprite.LayerMapSet(Layers.Base, sprite.AddLayerState("empty"));
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            // Update base item
            if (component.TryGetData(CellVisual.Occupied, out bool occupied))
            {
                // TODO: don't throw if it doesn't have a full state
                sprite.LayerSetState(Layers.Base, occupied ? "full" : "empty");
            }
            else
            {
                sprite.LayerSetState(Layers.Base, "empty");
            }
        }

        enum Layers
        {
            Base,
            Light,
        }
    }
}
