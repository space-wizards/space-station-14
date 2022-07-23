using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage.Components
{
    [RegisterComponent]
    [Access(typeof(SharedItemMapperSystem))]
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
