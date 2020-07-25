using System.Collections.Generic;
using System.IO;
using System.Linq;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Tools
{
    public class Map
    {
        public Map(YamlStream stream)
        {
            Stream = stream;
            Parse();
        }

        public Map(string path)
        {
            using var reader = new StreamReader(path);
            var yaml = new YamlStream();

            yaml.Load(reader);

            Stream = yaml;

            Parse();
        }

        public YamlStream Stream { get; }

        public YamlNode Root => Stream.Documents[0].RootNode;

        private Dictionary<uint, YamlMappingNode> Entities { get; set; }

        private uint MaxId { get; set; }

        private void AddEntity(YamlMappingNode node)
        {
            var uid = uint.Parse(node["uid"].AsString());

            if (uid > MaxId)
            {
                MaxId = uid;
            }
            else
            {
                uid = MaxId + 1;
                MaxId++;
                node.Children["uid"] = uid.ToString();
            }

            GetEntitiesNode().Add(node);
        }

        private YamlSequenceNode GetEntitiesNode()
        {
            return (YamlSequenceNode) Root["entities"];
        }

        private Dictionary<uint, YamlMappingNode> ParseEntities()
        {
            var entities = new Dictionary<uint, YamlMappingNode>();

            foreach (var entity in GetEntitiesNode())
            {
                var uid = uint.Parse(entity["uid"].AsString());
                entities[uid] = (YamlMappingNode) entity;
            }

            return entities;
        }

        private void Parse()
        {
            var oldMaxId = MaxId;

            Entities = ParseEntities();
            MaxId = Entities.Max(entry => entry.Key);

            DebugTools.Assert(oldMaxId == MaxId);
        }

        public void Merge(Map other)
        {
            foreach (var (id, otherEntity) in other.ParseEntities())
            {
                if (!Entities.TryGetValue(id, out var thisEntity) ||
                    !thisEntity.Equals(otherEntity))
                {
                    AddEntity(otherEntity);
                    return;
                }
            }

            Parse();
        }
    }
}
