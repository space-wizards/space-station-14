using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

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

        public DeserializationResult Read(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, bool skipHook, ISerializationContext? context = null)
        {
            //todo this read method does not support pushing inheritance
            var dictionary =
                serializationManager.ReadValueOrThrow<Dictionary<Vector2i, Dictionary<uint, Decal>>>(node, context, skipHook);

            var uids = new SortedSet<uint>();
            var uidChunkMap = new Dictionary<uint, Vector2i>();
            foreach (var (indices, decals) in dictionary)
            {
                foreach (var (uid, _) in decals)
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

            var newDict = new Dictionary<Vector2i, Dictionary<uint, Decal>>();
            foreach (var (oldUid, newUid) in uidMap)
            {
                var indices = uidChunkMap[oldUid];
                if(!newDict.ContainsKey(indices))
                    newDict[indices] = new();
                newDict[indices][newUid] = dictionary[indices][oldUid];
            }

            return new DeserializedValue<DecalGridComponent.DecalGridChunkCollection>(
                new DecalGridComponent.DecalGridChunkCollection(newDict){NextUid = nextIndex});
        }

        public DataNode Write(ISerializationManager serializationManager, DecalGridComponent.DecalGridChunkCollection value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return serializationManager.WriteValue(value.ChunkCollection, alwaysWrite, context);
        }

        public DecalGridComponent.DecalGridChunkCollection Copy(ISerializationManager serializationManager, DecalGridComponent.DecalGridChunkCollection source,
            DecalGridComponent.DecalGridChunkCollection target, bool skipHook, ISerializationContext? context = null)
        {
            var dict = serializationManager.Copy(source.ChunkCollection, target.ChunkCollection, context, skipHook)!;
            return new DecalGridComponent.DecalGridChunkCollection(dict) {NextUid = source.NextUid};
        }
    }
}
