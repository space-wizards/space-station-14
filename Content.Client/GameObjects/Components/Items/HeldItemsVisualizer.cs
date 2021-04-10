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
    public class HeldItemsVisualizer : AppearanceVisualizer
    {
        private List<string> _layerKeys = new();

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent<ISpriteComponent>(out var sprite)) return;
            if (!component.TryGetData(HeldItemsVisuals.VisualState, out HeldItemsVisualState visualState)) return;

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
            var heldItems = visualState.HeldItems;

            for (var i = 0; i < heldItems.Count; i++)
            {
                var item = heldItems[i];

                var rsi = resourceCache.GetResource<RSIResource>(SharedSpriteComponent.TextureRoot / item.RsiPath).RSI;

                var layerKey = "item" + i.ToString();
                _layerKeys.Add(layerKey);
                sprite.LayerMapReserveBlank(layerKey);

                var layer = sprite.LayerMapGet(layerKey);
                sprite.LayerSetVisible(layer, true);
                sprite.LayerSetRSI(layer, rsi);
                sprite.LayerSetState(layer, item.State);
                sprite.LayerSetColor(layer, item.Color);

            }
        }
    }

    public enum HeldItemsVisuals
    {
        VisualState
    }

    public class HeldItemsVisualState
    {
        public List<ItemVisualState> HeldItems { get; } = new();

        public HeldItemsVisualState(List<ItemVisualState> heldItems)
        {
            HeldItems = heldItems;
        }
    }

    public class ItemVisualState
    {
        [ViewVariables]
        public string RsiPath { get; }

        [ViewVariables]
        public string State { get; }

        [ViewVariables]
        public Color Color { get; }

        public ItemVisualState(string rsiPath, string state, Color color)
        {
            RsiPath = rsiPath;
            State = state;
            Color = color;
        }
    }
}
