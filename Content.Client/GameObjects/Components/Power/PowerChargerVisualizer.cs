using Content.Shared.GameObjects.Components.Power;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Client.GameObjects.Components.Power
{
    [UsedImplicitly]
    public class PowerChargerVisualizer : AppearanceVisualizer
    {
        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            var sprite = entity.GetComponent<ISpriteComponent>();

            // Base item
            sprite.LayerMapSet(Layers.Base, sprite.AddLayerState("empty"));

            // Light
            sprite.LayerMapSet(Layers.Light, sprite.AddLayerState("light-off"));
            sprite.LayerSetShader(Layers.Light, "unshaded");
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

            // Update lighting
            if (component.TryGetData(CellVisual.Light, out CellChargerStatus status))
            {
                switch (status)
                {
                    case CellChargerStatus.Off:
                        sprite.LayerSetState(Layers.Light, "light-off");
                        break;
                    case CellChargerStatus.Empty:
                        sprite.LayerSetState(Layers.Light, "light-empty");
                        break;
                    case CellChargerStatus.Charging:
                        sprite.LayerSetState(Layers.Light, "light-charging");
                        break;
                    case CellChargerStatus.Charged:
                        sprite.LayerSetState(Layers.Light, "light-charged");
                        break;
                    default:
                        sprite.LayerSetState(Layers.Light, "light-off");
                        break;
                }
            }
            else
            {
                sprite.LayerSetState(Layers.Light, "light-off");
            }
        }

        enum Layers : byte
        {
            Base,
            Light,
        }
    }
}
