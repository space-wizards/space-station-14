using System.Collections.Generic;
using System.IO;
using System.Linq;
using Robust.Shared.GameObjects.Systems;
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
            Entities = ParseEntities();
            MaxId = Entities.Max(entry => entry.Key);
        }

        public void Merge(Map other)
        {
            var maxId = MaxId;

            foreach (var (id, otherEntity) in other.ParseEntities())
            {
                if (!Entities.TryGetValue(id, out var thisEntity))
                {
                    GetEntitiesNode().Add(otherEntity);
                    return;
                }

                if (thisEntity.Equals(otherEntity))
                {
                    continue;
                }

                otherEntity.Children["uid"] = (maxId + 1).ToString();
                maxId++;

                GetEntitiesNode().Add(otherEntity);
            }

            Parse();

            DebugTools.Assert(maxId == MaxId);
        }
    }
}
