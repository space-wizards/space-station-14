using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Tools.Handlers
{
    public class EntitiesHandler
    {
        public EntitiesHandler(YamlNode root)
        {
            EntitiesNode = (YamlSequenceNode) root["entities"];
            Entities = ParseEntities();
        }

        private YamlSequenceNode EntitiesNode { get; }

        private Dictionary<uint, YamlMappingNode> Entities { get; }

        private uint MaxId => Entities.Max(entry => entry.Key);

        private Dictionary<uint, YamlMappingNode> ParseEntities()
        {
            var entities = new Dictionary<uint, YamlMappingNode>();

            foreach (var entity in EntitiesNode)
            {
                var uid = uint.Parse(entity["uid"].AsString());
                entities[uid] = (YamlMappingNode) entity;
            }

            return entities;
        }

        private void AddEntity(YamlMappingNode entity)
        {
            var uid = uint.Parse(entity["uid"].AsString());

            if (uid <= MaxId)
            {
                var oldUid = uid;
                uid = MaxId + 1;
                entity.Children["uid"] = uid.ToString(CultureInfo.InvariantCulture);
                SyncIds(EntitiesNode, oldUid, uid);
            }

            EntitiesNode.Add(entity);
            Entities.Add(uid, entity);
        }

        private void SyncIds(YamlNode node, uint old, uint @new)
        {
            switch (node)
            {
                case YamlSequenceNode subSequence:
                    SyncIds(subSequence, old, @new);
                    break;
                case YamlMappingNode subMapping:
                    SyncIds(subMapping, old, @new);
                    break;
                default:
                    throw new ArgumentException($"Unrecognized YAML node type: {node.GetType()}");
            }
        }

        private void SyncIds(YamlSequenceNode node, uint old, uint @new)
        {
            foreach (var subNode in node)
            {
                SyncIds(subNode, old, @new);
            }
        }

        private void SyncIds(YamlMappingNode node, uint old, uint @new)
        {
            foreach (var (subKey, subValue) in node)
            {
                // Don't replace an entity's UID, those are already taken care of
                // and made sure to not conflict
                if (subKey.AsString() == "uid")
                {
                    continue;
                }

                if (!(subValue is YamlScalarNode subScalar))
                {
                    SyncIds(subValue, old, @new);
                    continue;
                }

                // TODO: Make sure it's actually an entity UID
                if (!uint.TryParse(subScalar.AsString(), out var uid))
                {
                    continue;
                }

                if (uid != old)
                {
                    continue;
                }

                subScalar.Value = @new.ToString();
            }
        }

        public void Merge(Map map)
        {
            foreach (var (id, otherEntity) in map.EntitiesHandler.Entities)
            {
                if (!Entities.TryGetValue(id, out var thisEntity) ||
                    !thisEntity.Equals(otherEntity))
                {
                    AddEntity(otherEntity);
                    return;
                }
            }
        }
    }
}
