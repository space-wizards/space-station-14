using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Storage.Components
{
    [RegisterComponent]
    public class ItemMapperComponent : Component, ISerializationHooks
    {
        public override string Name => "ItemMapper";

        [DataField("mapLayers")] public readonly Dictionary<string, SharedMapLayerData> MapLayers = new();

        void ISerializationHooks.AfterDeserialization()
        {
            foreach (var (layerName, val) in MapLayers)
            {
                val.Layer = layerName;
            }
        }

    }
}
