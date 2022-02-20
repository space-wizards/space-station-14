using System;
using System.Collections.Generic;
using Content.Shared.Gravity;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Gravity
{
    [UsedImplicitly]
    public sealed class GravityGeneratorVisualizer : AppearanceVisualizer, ISerializationHooks
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

        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out SpriteComponent? sprite))
                return;

            sprite.LayerMapReserveBlank(GravityGeneratorVisualLayers.Base);
            sprite.LayerMapReserveBlank(GravityGeneratorVisualLayers.Core);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<SpriteComponent>(component.Owner);

            if (component.TryGetData(GravityGeneratorVisuals.State, out GravityGeneratorStatus state))
            {
                if (_spriteMap.TryGetValue(state, out var spriteState))
                {
                    var layer = sprite.LayerMapGet(GravityGeneratorVisualLayers.Base);
                    sprite.LayerSetState(layer, spriteState);
                }
            }

            if (component.TryGetData(GravityGeneratorVisuals.Charge, out float charge))
            {
                var layer = sprite.LayerMapGet(GravityGeneratorVisualLayers.Core);
                switch (charge)
                {
                    case < 0.2f:
                        sprite.LayerSetVisible(layer, false);
                        break;
                    case >= 0.2f and < 0.4f:
                        sprite.LayerSetVisible(layer, true);
                        sprite.LayerSetState(layer, "startup");
                        break;
                    case >= 0.4f and < 0.6f:
                        sprite.LayerSetVisible(layer, true);
                        sprite.LayerSetState(layer, "idle");
                        break;
                    case >= 0.6f and < 0.8f:
                        sprite.LayerSetVisible(layer, true);
                        sprite.LayerSetState(layer, "activating");
                        break;
                    default:
                        sprite.LayerSetVisible(layer, true);
                        sprite.LayerSetState(layer, "activated");
                        break;
                }
            }
        }

        public enum GravityGeneratorVisualLayers : byte
        {
            Base,
            Core
        }
    }
}
