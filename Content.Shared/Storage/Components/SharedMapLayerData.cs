using System;
using System.Collections.Generic;
using Content.Shared.Whitelist;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Storage.Components
{
    [Serializable, NetSerializable]
    public enum StorageMapVisuals : sbyte
    {
        InitLayers,
        LayerChanged,
    }

    [Serializable]
    [DataDefinition]
    public class SharedMapLayerData
    {
        public string Layer = string.Empty;

        [DataField("whitelist", required: true)]
        public EntityWhitelist Whitelist { get; set; } = new();
    }

    [Serializable, NetSerializable]
    public class ShowLayerData : ICloneable
    {
        public IReadOnlyList<string> QueuedEntities { get; internal set; }

        public ShowLayerData()
        {
            QueuedEntities = new List<string>();
        }

        public ShowLayerData(IEnumerable<string> other)
        {
            QueuedEntities = new List<string>(other);
        }

        public object Clone()
        {
            return new ShowLayerData(QueuedEntities);
        }
    }
}
