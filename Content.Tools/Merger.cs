using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Content.Tools
{
    public class Merger
    {
        public Map MapOurs { get; }
        public Map MapBased { get; }
        public Map MapOther { get; }

        public Dictionary<uint, uint> TileMapFromOtherToOurs { get; } = new Dictionary<uint, uint>();
        public Dictionary<uint, uint> EntityMapFromOtherToOurs { get; } = new Dictionary<uint, uint>();
        public List<uint> EntityListDirectMerge { get; } = new List<uint>();

        public Merger(Map ours, Map based, Map other)
        {
            MapOurs = ours;
            MapBased = based;
            MapOther = other;
        }

        public bool Merge()
        {
            PlanTileMapping();
            if (!MergeTiles())
                return false;
            PlanEntityMapping();
            bool success = true;
            foreach (var kvp in EntityMapFromOtherToOurs)
            {
                YamlMappingNode oursEnt;
                YamlMappingNode basedEnt;
                if (MapOurs.Entities.ContainsKey(kvp.Value))
                {
                    oursEnt = MapOurs.Entities[kvp.Value];
                    if (MapBased.Entities.ContainsKey(kvp.Value))
                    {
                        basedEnt = MapBased.Entities[kvp.Value];
                    }
                    else
                    {
                        basedEnt = oursEnt;
                    }
                }
                else
                {
                    basedEnt = oursEnt = new YamlMappingNode();
                    MapOurs.Entities[kvp.Value] = basedEnt;
                }
                if (!MergeEntityNodes(oursEnt, basedEnt, MapOther.Entities[kvp.Key]))
                {
                    Console.WriteLine("Unable to successfully merge entity " + kvp.Key);
                    success = false;
                }
                oursEnt.Children["uid"] = kvp.Value.ToString(CultureInfo.InvariantCulture);
            }
            if (!success)
                return false;
            return true;
        }

        // -- Tiles --

        public void PlanTileMapping()
        {
            // TODO
        }

        public bool MergeTiles()
        {
            // TODO
            return true;
        }

        // -- Entities --

        public void PlanEntityMapping()
        {
            // Ok, so here's how it works:
            // 1. Entities that do not exist in "based" are additions.
            // 2. Entities that exist in "based" but do not exist in the one map or the other are removals.

            // Find modifications and deletions
            foreach (var kvp in MapBased.Entities)
            {
                var deletedByOurs = !MapOurs.Entities.ContainsKey(kvp.Key);
                var deletedByOther = !MapOther.Entities.ContainsKey(kvp.Key);
                if (deletedByOther && (!deletedByOurs))
                {
                    // Delete
                    MapOurs.Entities.Remove(kvp.Key);
                }
                else if (!(deletedByOurs || deletedByOther))
                {
                    // Modify
                    EntityMapFromOtherToOurs[kvp.Key] = kvp.Key;
                }
            }

            // Find additions
            foreach (var kvp in MapOther.Entities)
            {
                if (!MapBased.Entities.ContainsKey(kvp.Key))
                {
                    // New
                    var newId = MapOurs.NextAvailableEntityId++;
                    EntityMapFromOtherToOurs[kvp.Key] = newId;
                }
            }
        }

        public bool MergeEntityNodes(YamlMappingNode ours, YamlMappingNode based, YamlMappingNode other)
        {
            // Copy to intermmediate
            var otherMapped = (YamlMappingNode) YamlTools.CopyYamlNodes(other);
            if (!MapEntity(otherMapped))
                return false;
            // Merge
            YamlTools.MergeYamlNodes(ours, based, otherMapped, "Entity" + (other["uid"].ToString()));
            return true;
        }

        public bool MapEntity(YamlMappingNode other)
        {
            var path = "Entity" + (other["uid"].ToString());
            if (other.Children.ContainsKey("components"))
            {
                var components = (YamlSequenceNode) other["components"];
                foreach (var component in components)
                {
                    var type = component["type"].ToString();
                    if (type == "Transform")
                    {
                        if (!MapEntityProperty((YamlMappingNode) component, "parent", path))
                            return false;
                    }
                    else if (type == "ContainerContainer")
                    {
                        MapEntityRecursiveAndBadly(component, path);
                    }
                }
            }
            return true;
        }

        public bool MapEntityProperty(YamlMappingNode node, string property, string path)
        {
            if (node.Children.ContainsKey(property)) {
                var prop = node[property];
                if (prop is YamlScalarNode)
                    return MapEntityProperty((YamlScalarNode) prop, path + "/" + property);
            }
            return true;
        }

        public bool MapEntityProperty(YamlScalarNode node, string path)
        {
            if (uint.TryParse(node.ToString(), out var uid))
            {
                if (EntityMapFromOtherToOurs.ContainsKey(uid))
                {
                    node.Value = EntityMapFromOtherToOurs[uid].ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    Console.WriteLine($"Error finding UID in MapEntityRecursiveAndBadly {path}. To fix this, the merge driver needs to be improved.");
                    return false;
                }
            }
            return true;
        }

        public bool MapEntityRecursiveAndBadly(YamlNode node, string path)
        {
            switch (node)
            {
                case YamlSequenceNode subSequence:
                    var idx = 0;
                    foreach (var val in subSequence)
                        if (!MapEntityRecursiveAndBadly(val, path + "/" + (idx++)))
                            return false;
                    return true;
                case YamlMappingNode subMapping:
                    foreach (var kvp in subMapping)
                        if (!MapEntityRecursiveAndBadly(kvp.Key, path))
                            return false;
                    foreach (var kvp in subMapping)
                        if (!MapEntityRecursiveAndBadly(kvp.Value, path + "/" + kvp.Key.ToString()))
                            return false;
                    return true;
                case YamlScalarNode subScalar:
                    return MapEntityProperty(subScalar, path);
                default:
                    throw new ArgumentException($"Unrecognized YAML node type: {node.GetType()} at {path}");
            }
        }
    }
}
