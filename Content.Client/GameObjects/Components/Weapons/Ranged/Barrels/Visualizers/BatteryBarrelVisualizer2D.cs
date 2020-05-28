using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Weapons.Ranged.Barrels.Visualizers
{
    [UsedImplicitly]
    public sealed class BatteryBarrelVisualizer2D : AppearanceVisualizer
    {
        private bool _emptyState;
        private string _baseState;
        private string _batteryState;
        private int _batterySteps;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);
            _baseState = node.GetNode("baseState").AsString();
            _batteryState = node.GetNode("batteryState").AsString();
            _batterySteps = node.GetNode("batterySteps").AsInt();
            _emptyState = node.GetNode("emptyState").AsBool();
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);
            var sprite = entity.GetComponent<ISpriteComponent>();
            sprite.LayerSetState(BatteryBarrelVisualLayers.Base, _baseState);
            sprite.LayerSetState(BatteryBarrelVisualLayers.Battery, $"{_batteryState}-1");
            sprite.LayerSetVisible(BatteryBarrelVisualLayers.Battery, false);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            component.TryGetData(BatteryBarrelVisuals.BatteryLoaded, out bool batteryLoaded);

            if (batteryLoaded)
            {
                sprite.LayerSetVisible(BatteryBarrelVisualLayers.Battery, true);
                
                if (!component.TryGetData(BatteryBarrelVisuals.AmmoMax, out int capacity))
                {
                    return;
                }
                if (!component.TryGetData(BatteryBarrelVisuals.AmmoCount, out int current))
                {
                    return;
                }
                
                var step = ContentHelpers.RoundToLevels(current, capacity, _batterySteps);

                if (step == 0 && !_emptyState)
                {
                    sprite.LayerSetVisible(BatteryBarrelVisualLayers.Battery, false);
                }
                else
                {
                    sprite.LayerSetState(BatteryBarrelVisualLayers.Battery, $"{_batteryState}-{step}");
                }
            }
            else
            {
                sprite.LayerSetVisible(BatteryBarrelVisualLayers.Battery, false);
            }
        }
    }

    public enum BatteryBarrelVisualLayers
    {
        Base,
        Battery,
    }
}