using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Storage
{
    [RegisterComponent]
    public class SharedItemCounterComponent : Component, ISerializationHooks
    {
        public override string Name => "ItemCounter";

        [DataField("mapLayers")] public readonly List<LayerProperties> _mapLayers = new();
        public IReadOnlyList<string> SpriteLayers = new List<string>();

        [Serializable]
        [DataDefinition]
        public struct LayerProperties
        {
            [DataField("layer")] public string Layer;
            [DataField("ids")] public List<string>? Id { get; set; }
            [DataField("tags")] public List<string>? Tags { get; set; }
        }

        void ISerializationHooks.AfterDeserialization()
        {
            var allLayers = new List<string>();
            foreach (var mapLayer in _mapLayers)
            {
                if (!allLayers.Contains(mapLayer.Layer))
                {
                    allLayers.Add(mapLayer.Layer);
                }
            }

            SpriteLayers = allLayers;
        }
    }
}
