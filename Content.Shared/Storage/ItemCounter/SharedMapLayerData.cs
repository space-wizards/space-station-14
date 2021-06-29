using System;
using System.Collections.Generic;
using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Storage.ItemCounter
{
    [Serializable, NetSerializable]
    public enum StorageMapVisuals : sbyte
    {
        AllLayers,
        LayerChanged,
    }

    [Serializable]
    [DataDefinition]
    public struct SharedMapLayerData
    {
        [DataField("layer")] public string Layer;
        [DataField("whitelist", required: true)] public EntityWhitelist Whitelist { get; set; }
    }

    [Serializable, NetSerializable]
    public class ShowEntityData
    {
        public IReadOnlyList<EntityUid> QueuedEntities { get; internal set; }

        public ShowEntityData()
        {
            QueuedEntities = new List<EntityUid>();
        }

        public ShowEntityData(IReadOnlyList<EntityUid> other)
        {
            QueuedEntities = other;
        }
        
        public ShowEntityData(ShowEntityData other)
        {
            QueuedEntities = other.QueuedEntities;
        }
    }
}
