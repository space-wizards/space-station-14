#nullable enable
using Content.Shared.GameObjects.Components.Gravity;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Gravity
{
    [UsedImplicitly]
    public class GravityGeneratorVisualizer : AppearanceVisualizer
    {
        private readonly Dictionary<GravityGeneratorStatus, string> _spriteMap = new();

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (!entity.TryGetComponent(out SpriteComponent? sprite))
                return;

            sprite.LayerMapReserveBlank(GravityGeneratorVisualLayers.Base);
            sprite.LayerMapReserveBlank(GravityGeneratorVisualLayers.Core);
        }

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            // Get Sprites for each status
            foreach (var status in (GravityGeneratorStatus[]) Enum.GetValues(typeof(GravityGeneratorStatus)))
            {
                if (node.TryGetNode(status.ToString().ToLower(), out var sprite))
                {
                    _spriteMap[status] = sprite.AsString();
                }
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<SpriteComponent>();

            if (component.TryGetData(GravityGeneratorVisuals.State, out GravityGeneratorStatus state))
            {
                if (_spriteMap.TryGetValue(state, out var spriteState))
                {
                    var layer = sprite.LayerMapGet(GravityGeneratorVisualLayers.Base);
                    sprite.LayerSetState(layer, spriteState);
                }
            }

            if (component.TryGetData(GravityGeneratorVisuals.CoreVisible, out bool visible))
            {
                var layer = sprite.LayerMapGet(GravityGeneratorVisualLayers.Core);
                sprite.LayerSetVisible(layer, visible);
            }
        }

        public enum GravityGeneratorVisualLayers : byte
        {
            Base,
            Core
        }
    }
}
