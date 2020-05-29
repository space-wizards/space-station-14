using System;
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
    public sealed class BarrelMagVisualizer2D : AppearanceVisualizer
    {
        private string _magState;
        private int _magSteps;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);
            _magState = node.GetNode("magState").AsString();
            _magSteps = node.GetNode("steps").AsInt();
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);
            var sprite = entity.GetComponent<ISpriteComponent>();

            if (sprite.LayerMapTryGet(RangedBarrelVisualLayers.Mag, out _))
            {
                sprite.LayerSetState(RangedBarrelVisualLayers.Mag, $"{_magState}-0");
                sprite.LayerSetVisible(RangedBarrelVisualLayers.Mag, false);
            }

            if (sprite.LayerMapTryGet(RangedBarrelVisualLayers.MagUnshaded, out _))
            {
                sprite.LayerSetState(RangedBarrelVisualLayers.MagUnshaded, $"{_magState}-unshaded-0");
                sprite.LayerSetVisible(RangedBarrelVisualLayers.MagUnshaded, false);
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            component.TryGetData(MagazineBarrelVisuals.MagLoaded, out bool magLoaded);

            if (magLoaded)
            {
                if (!component.TryGetData(AmmoVisuals.AmmoMax, out int capacity))
                {
                    return;
                }
                if (!component.TryGetData(AmmoVisuals.AmmoCount, out int current))
                {
                    return;
                }
                
                var step = ContentHelpers.RoundToLevels(current, capacity, _magSteps);

                if (sprite.LayerMapTryGet(RangedBarrelVisualLayers.Mag, out _))
                {
                    sprite.LayerSetVisible(RangedBarrelVisualLayers.Mag, true);
                    sprite.LayerSetState(RangedBarrelVisualLayers.Mag, $"{_magState}-{step}");
                }

                if (sprite.LayerMapTryGet(RangedBarrelVisualLayers.MagUnshaded, out _))
                {
                    sprite.LayerSetVisible(RangedBarrelVisualLayers.MagUnshaded, true);
                    sprite.LayerSetState(RangedBarrelVisualLayers.MagUnshaded, $"{_magState}-unshaded-{step}");
                }
            }
            else
            {
                if (sprite.LayerMapTryGet(RangedBarrelVisualLayers.Mag, out _))
                {
                    sprite.LayerSetVisible(RangedBarrelVisualLayers.Mag, false);
                }
                
                if (sprite.LayerMapTryGet(RangedBarrelVisualLayers.MagUnshaded, out _))
                {
                    sprite.LayerSetVisible(RangedBarrelVisualLayers.MagUnshaded, false);
                }
            }
        }
    }
}