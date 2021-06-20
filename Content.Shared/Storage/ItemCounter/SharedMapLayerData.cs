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
        public EntityUid Uid { get; }
        public bool Show { get; }

        public ShowEntityData(EntityUid uid, bool show)
        {
            Uid = uid;
            Show = show;
        }
    }

    [Serializable, NetSerializable]
    public record ListOfUids(List<EntityUid> ContainedEntities)
    {
    }
}
