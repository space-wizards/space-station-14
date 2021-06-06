using Content.Shared.GameObjects.Components.Items;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Content.Client.GameObjects.Components.Items
{
    [UsedImplicitly]
    public class HandsVisualizer : AppearanceVisualizer
    {
        private List<string> _layerKeys = new();

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent<ISpriteComponent>(out var sprite)) return;
            if (!component.TryGetData(HandsVisuals.VisualState, out HandsVisualState visualState)) return;

            foreach (var layerKey in _layerKeys)
            {
                if (sprite.LayerMapTryGet(layerKey, out var layer))
                {
                    sprite.RemoveLayer(layer);
                    sprite.LayerMapRemove(layer);
                }
            }
            _layerKeys.Clear();

            var resourceCache = IoCManager.Resolve<IResourceCache>();
            var hands = visualState.Hands;

            for (var i = 0; i < hands.Count; i++)
            {
                var hand = hands[i];

                var rsi = resourceCache.GetResource<RSIResource>(SharedSpriteComponent.TextureRoot / hand.RsiPath).RSI;

                var layerKey = "item" + i.ToString();
                _layerKeys.Add(layerKey);
                sprite.LayerMapReserveBlank(layerKey);

                var layer = sprite.LayerMapGet(layerKey);
                sprite.LayerSetVisible(layer, true);
                sprite.LayerSetRSI(layer, rsi);
                sprite.LayerSetColor(layer, hand.Color);

                var state = $"inhand-{hand.Location.ToString().ToLowerInvariant()}";
                if (hand.EquippedPrefix != null)
                    state = $"{hand.EquippedPrefix}-" + state;

                sprite.LayerSetState(layer, state);
            }
        }
    }

    public enum HandsVisuals
    {
        VisualState
    }

    public class HandsVisualState
    {
        public List<HandVisualState> Hands { get; } = new();

        public HandsVisualState(List<HandVisualState> hands)
        {
            Hands = hands;
        }
    }

    public class HandVisualState
    {
        public string RsiPath { get; }

        public string? EquippedPrefix { get; }

        public HandLocation Location { get; }

        public Color Color { get; }

        public HandVisualState(string rsiPath, string? equippedPrefix, HandLocation location, Color color)
        {
            RsiPath = rsiPath;
            EquippedPrefix = equippedPrefix;
            Location = location;
            Color = color;
        }
    }
}
