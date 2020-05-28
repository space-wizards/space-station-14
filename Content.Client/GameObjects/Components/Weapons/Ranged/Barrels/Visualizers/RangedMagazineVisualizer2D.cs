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
    public sealed class RangedMagazineVisualizer2D : AppearanceVisualizer
    {
        private string _magState;
        private int _steps;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);
            _magState = node.GetNode("magState").AsString();
            // +1 just to make the yaml slightly more readable as 0 state is just made invisible
            _steps = node.GetNode("steps").AsInt() + 1;
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);
            var sprite = entity.GetComponent<ISpriteComponent>();
            // A mag might only have an unshaded layer
            if (sprite.LayerMapTryGet(RangedMagazineVisualLayers.Mag, out _))
            {
                sprite.LayerSetState(RangedMagazineVisualLayers.Mag, $"{_magState}-1");
            }

            if (sprite.LayerMapTryGet(RangedMagazineVisualLayers.MagUnshaded, out _))
            {
                sprite.LayerSetState(RangedMagazineVisualLayers.MagUnshaded, $"{_magState}-unshaded-1");
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            
            if (!component.TryGetData(AmmoVisuals.AmmoMax, out int capacity))
            {
                return;
            }
            if (!component.TryGetData(AmmoVisuals.AmmoCount, out int current))
            {
                return;
            }
            
            var step = ContentHelpers.RoundToLevels(current, capacity, _steps);
            
            // If no rounds left we'll just hide the mag layer
            if (step == 0)
            {
                if (sprite.LayerMapTryGet(RangedMagazineVisualLayers.Mag, out _))
                {
                    sprite.LayerSetVisible(RangedMagazineVisualLayers.Mag, false);
                }
                
                if (sprite.LayerMapTryGet(RangedMagazineVisualLayers.MagUnshaded, out _))
                {
                    sprite.LayerSetVisible(RangedMagazineVisualLayers.MagUnshaded, false);
                }

                return;
            }

            if (sprite.LayerMapTryGet(RangedMagazineVisualLayers.Mag, out _))
            {
                sprite.LayerSetState(RangedMagazineVisualLayers.Mag, $"{_magState}-{step}");
            }
            
            if (sprite.LayerMapTryGet(RangedMagazineVisualLayers.MagUnshaded, out _))
            {
                sprite.LayerSetState(RangedMagazineVisualLayers.MagUnshaded, $"{_magState}-unshaded-{step}");
            }
        }
    }
    
    public enum RangedMagazineVisualLayers
    {
        Base,
        Mag,
        MagUnshaded,
    }
}