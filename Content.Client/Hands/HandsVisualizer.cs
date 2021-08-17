using Content.Shared.Hands.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using System;
using System.Collections.Generic;

namespace Content.Client.Hands
{
    [UsedImplicitly]
    public class HandsVisualizer : AppearanceVisualizer
    {

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent<ISpriteComponent>(out var sprite)) return;
            if (!component.TryGetData(HandsVisuals.VisualState, out HandsVisualState visualState)) return;

            foreach (HandLocation location in Enum.GetValues(typeof(HandLocation)))
            {
                var layerKey = LocationToLayerKey(location);
                if (sprite.LayerMapTryGet(layerKey, out var layer))
                {
                    sprite.RemoveLayer(layer);
                    sprite.LayerMapRemove(layer);
                }
            }

            var resourceCache = IoCManager.Resolve<IResourceCache>();
            var hands = visualState.Hands;

            foreach (var hand in hands)
            {
                var rsi = resourceCache.GetResource<RSIResource>(SharedSpriteComponent.TextureRoot / hand.RsiPath).RSI;

                var state = $"inhand-{hand.Location.ToString().ToLowerInvariant()}";
                if (hand.EquippedPrefix != null)
                    state = $"{hand.EquippedPrefix}-" + state;

                if (rsi.TryGetState(state, out var _))
                {
                    var layerKey = LocationToLayerKey(hand.Location);
                    sprite.LayerMapReserveBlank(layerKey);

                    var layer = sprite.LayerMapGet(layerKey);
                    sprite.LayerSetVisible(layer, true);
                    sprite.LayerSetRSI(layer, rsi);
                    sprite.LayerSetColor(layer, hand.Color);
                    sprite.LayerSetState(layer, state);
                }
            }
        }

        private string LocationToLayerKey(HandLocation location)
        {
            return location.ToString();
        }
    }

    public enum HandsVisuals : byte
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
