using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using static Content.Shared.Decals.DecalGridComponent;

namespace Content.Shared.Decals
{
    [TypeSerializer]
    public sealed class DecalGridChunkCollectionTypeSerializer : ITypeSerializer<DecalGridComponent.DecalGridChunkCollection, MappingDataNode>
    {
        public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            return serializationManager.ValidateNode<Dictionary<Vector2i, Dictionary<uint, Decal>>>(node, context);
        }

        public DecalGridComponent.DecalGridChunkCollection Read(ISerializationManager serializationManager,
            MappingDataNode node,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<DecalGridComponent.DecalGridChunkCollection>? _ = default)
        {
            var dictionary = serializationManager.Read<Dictionary<Vector2i, DecalChunk>>(node, hookCtx, context, notNullableOverride: true);

            var uids = new SortedSet<uint>();
            var uidChunkMap = new Dictionary<uint, Vector2i>();
            foreach (var (indices, decals) in dictionary)
            {
                foreach (var uid in decals.Decals.Keys)
                {
                    uids.Add(uid);
                    uidChunkMap[uid] = indices;
                }
            }

            var uidMap = new Dictionary<uint, uint>();
            uint nextIndex = 0;
            foreach (var uid in uids)
            {
                uidMap[uid] = nextIndex++;
            }

            var newDict = new Dictionary<Vector2i, DecalChunk>();
            foreach (var (oldUid, newUid) in uidMap)
            {
                var indices = uidChunkMap[oldUid];
                if(!newDict.ContainsKey(indices))
                    newDict[indices] = new();
                newDict[indices].Decals[newUid] = dictionary[indices].Decals[oldUid];
            }

            return new DecalGridComponent.DecalGridChunkCollection(newDict){NextUid = nextIndex};
        }

        public DataNode Write(ISerializationManager serializationManager,
            DecalGridComponent.DecalGridChunkCollection value, IDependencyCollection dependencies,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return serializationManager.WriteValue(value.ChunkCollection, alwaysWrite, context, notNullableOverride: true);
        }
    }
}
