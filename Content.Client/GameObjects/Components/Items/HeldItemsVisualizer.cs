using Content.Shared.GameObjects.Components.Items;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;

namespace Content.Client.GameObjects.Components.Items
{
    [UsedImplicitly]
    public class HeldItemsVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent<ISpriteComponent>(out var sprite)) return;
            if (!component.TryGetData(HeldItemsVisuals.VisualState, out HeldItemsVisualState state)) return;

            //TODO
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
        public string? RsiPath { get; }

        [ViewVariables]
        public string? State { get; }

        [ViewVariables]
        public Color Color { get; }

        public ItemVisualState(string? rsiPath, string? state, Color color)
        {
            RsiPath = rsiPath;
            State = state;
            Color = color;
        }
    }

    /*private List<string> _handLayers = new(); //TODO: Replace with visualizer

    private void RemoveHandLayers() //TODO: Replace with visualizer
    {
        if (_sprite == null)
            return;

        foreach (var layerKey in _handLayers)
        {
            var layer = _sprite.LayerMapGet(layerKey);
            _sprite.RemoveLayer(layer);
            _sprite.LayerMapRemove(layerKey);
        }
        _handLayers.Clear();
    }

    private void MakeHandLayers() //TODO: Replace with visualizer
    {
        if (_sprite == null)
            return;

        foreach (var hand in ReadOnlyHands)
        {
            var key = $"hand-{hand.Name}";
            _sprite.LayerMapReserveBlank(key);

            var heldEntity = hand.HeldEntity;
            if (heldEntity == null || !heldEntity.TryGetComponent(out ItemComponent? item))
                continue;

            var maybeInHands = item.GetInHandStateInfo(hand.Location);
            if (maybeInHands == null)
                continue;

            var (rsi, state, color) = maybeInHands.Value;

            if (rsi == null)
            {
                _sprite.LayerSetVisible(key, false);
            }
            else
            {
                _sprite.LayerSetColor(key, color);
                _sprite.LayerSetVisible(key, true);
                _sprite.LayerSetState(key, state, rsi);
            }
            _handLayers.Add(key);
        }
    }*/
}
