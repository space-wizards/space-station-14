using System.Collections.Generic;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Storage.Components
{
    [RegisterComponent]
    [Friend(typeof(SharedItemMapperSystem))]
    public sealed class ItemMapperComponent : Component, ISerializationHooks
    {
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
