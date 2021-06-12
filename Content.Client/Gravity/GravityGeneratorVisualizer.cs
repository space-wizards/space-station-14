#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.Gravity;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Gravity
{
    [UsedImplicitly]
    public class GravityGeneratorVisualizer : AppearanceVisualizer, ISerializationHooks
    {
        [DataField("spritemap")]
        private Dictionary<string, string> _rawSpriteMap = new();
        private Dictionary<GravityGeneratorStatus, string> _spriteMap = new();

        void ISerializationHooks.BeforeSerialization()
        {
            _rawSpriteMap = new Dictionary<string, string>();
            foreach (var (status, sprite) in _spriteMap)
            {
                _rawSpriteMap.Add(status.ToString().ToLower(), sprite);
            }
        }

        void ISerializationHooks.AfterDeserialization()
        {
            // Get Sprites for each status
            foreach (var status in (GravityGeneratorStatus[]) Enum.GetValues(typeof(GravityGeneratorStatus)))
            {
                if (_rawSpriteMap.TryGetValue(status.ToString().ToLower(), out var sprite))
                {
                    _spriteMap[status] = sprite;
                }
            }
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (!entity.TryGetComponent(out SpriteComponent? sprite))
                return;

            sprite.LayerMapReserveBlank(GravityGeneratorVisualLayers.Base);
            sprite.LayerMapReserveBlank(GravityGeneratorVisualLayers.Core);
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
