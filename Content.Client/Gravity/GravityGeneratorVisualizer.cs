using System.Linq;
using Content.Shared.Gravity;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Gravity
{
    [UsedImplicitly]
    public sealed class GravityGeneratorVisualizer : AppearanceVisualizer
    {
        [DataField("spritemap")]
        private Dictionary<string, string> _rawSpriteMap
        {
            get => _spriteMap.ToDictionary(x => x.Key.ToString().ToLower(), x => x.Value);
            set
            {
                _spriteMap.Clear();
                // Get Sprites for each status
                foreach (var status in (GravityGeneratorStatus[]) Enum.GetValues(typeof(GravityGeneratorStatus)))
                {
                    if (value.TryGetValue(status.ToString().ToLower(), out var sprite))
                    {
                        _spriteMap[status] = sprite;
                    }
                }
            }
        }

        private Dictionary<GravityGeneratorStatus, string> _spriteMap = new();

        [Obsolete("Subscribe to your component being initialised instead.")]
        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out SpriteComponent? sprite))
                return;

            sprite.LayerMapReserveBlank(GravityGeneratorVisualLayers.Base);
            sprite.LayerMapReserveBlank(GravityGeneratorVisualLayers.Core);
        }

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
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
