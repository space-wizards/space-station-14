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

        private void AddEntity(YamlMappingNode node)
        {
            var uid = uint.Parse(node["uid"].AsString());

            if (uid <= MaxId)
            {
                uid = MaxId + 1;
                node.Children["uid"] = uid.ToString(CultureInfo.InvariantCulture);
                // TODO: Sync references
            }

            EntitiesNode.Add(node);
            Entities.Add(uid, node);
        }

        public void Merge(Map map)
        {
            foreach (var (id, otherEntity) in map.EntitiesHandler.Entities)
            {
                if (!Entities.TryGetValue(id, out var thisEntity) ||
                    !thisEntity.Equals(otherEntity)) // TODO: Better equals
                {
                    AddEntity(otherEntity);
                    return;
                }
            }
        }
    }
}
