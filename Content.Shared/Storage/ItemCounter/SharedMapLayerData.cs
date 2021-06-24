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
        [DataField("id")] public string? Id;
        [DataField("whitelist")] public EntityWhitelist? Whitelist { get; set; }
    }

    [Serializable, NetSerializable]
    public class ShowEntityData
    {
        public List<(EntityUid, bool)> QueuedEntities { get; }

        public ShowEntityData()
        {
            QueuedEntities = new();
        }

        public ShowEntityData(ShowEntityData other)
        {
            QueuedEntities = new List<(EntityUid, bool)>(other.QueuedEntities);
        }
    }
}
