using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Storage.ItemCounter
{
    [RegisterComponent]
    public class ItemCounterComponent : Component, ISerializationHooks
    {
        public override string Name => "ItemCounter";

        public readonly Dictionary<string, SharedMapLayerData> SpriteLayers = new();
        [DataField("mapLayers")] private readonly List<SharedMapLayerData> _mapLayers = new();

        void ISerializationHooks.AfterDeserialization()
        {
            if (_mapLayers is { Count: > 0 })
            {
                foreach (var layerProp in _mapLayers)
                {
                    if (!SpriteLayers.TryAdd(layerProp.Layer, layerProp))
                    {
                        Logger.Warning($"Already added layer with name = `${layerProp.Layer}` skipping over");
                    }
                }
            }
        }
    }
}
