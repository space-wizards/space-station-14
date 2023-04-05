using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using YamlDotNet.RepresentationModel;

namespace Content.Tools
{
    public sealed class Merger
    {
        public Map MapOurs { get; }
        public Map MapBased { get; }
        public Map MapOther { get; }

        public Dictionary<uint, uint> TileMapFromOtherToOurs { get; } = new Dictionary<uint, uint>();
        public Dictionary<uint, uint> TileMapFromBasedToOurs { get; } = new Dictionary<uint, uint>();
        public Dictionary<uint, uint> EntityMapFromOtherToOurs { get; } = new Dictionary<uint, uint>();
        public List<uint> EntityListDirectMerge { get; } = new List<uint>();

        private const int ExpectedChunkSize = 16 * 16 * 4;

        public Merger(Map ours, Map based, Map other)
        {
            MapOurs = ours;
            MapBased = based;
            MapOther = other;
        }

        public bool Merge()
        {
            PlanTileMapping(TileMapFromOtherToOurs, MapOther);
            PlanTileMapping(TileMapFromBasedToOurs, MapBased);
            MergeTiles();
            PlanEntityMapping();
            return MergeEntities();
        }

        // -- Tiles --

        public void PlanTileMapping(Dictionary<uint, uint> relativeOtherToOurs, Map relativeOther)
        {
            var mapping = new Dictionary<string, uint>();
            uint nextAvailable = 0;
            foreach (var kvp in MapOurs.TilemapNode)
            {
                var k = uint.Parse(kvp.Key.ToString());
                var v = kvp.Value.ToString();
                mapping[v] = k;
                if (k >= nextAvailable)
                    nextAvailable = k + 1;
            }
            foreach (var kvp in relativeOther.TilemapNode)
            {
                var k = uint.Parse(kvp.Key.ToString());
                var v = kvp.Value.ToString();
                if (mapping.ContainsKey(v))
                {
                    relativeOtherToOurs[k] = mapping[v];
                }
                else
                {
                    MapOurs.TilemapNode.Add(nextAvailable.ToString(CultureInfo.InvariantCulture), v);
                    relativeOtherToOurs[k] = nextAvailable++;
                }
            }
        }

        public void MergeTiles()
        {
            var a = MapOurs.GridsNode.Children[0];
            var b = MapBased.GridsNode.Children[0];
            var c = MapOther.GridsNode.Children[0];
            var aChunks = a["chunks"];
            var bChunks = b["chunks"];
            var cChunks = c["chunks"];
            MergeTileChunks((YamlSequenceNode) aChunks, (YamlSequenceNode) bChunks, (YamlSequenceNode) cChunks);
        }

        public void MergeTileChunks(YamlSequenceNode aChunks, YamlSequenceNode bChunks, YamlSequenceNode cChunks)
        {
            var aMap = ConvertTileChunks(aChunks);
            var bMap = ConvertTileChunks(bChunks);
            var cMap = ConvertTileChunks(cChunks);

            var xMap = new HashSet<string>();
            foreach (var kvp in aMap)
                xMap.Add(kvp.Key);
            // don't include b because that would mess with chunk deletion
            foreach (var kvp in cMap)
                xMap.Add(kvp.Key);

            foreach (var ind in xMap)
            {
                using var a = new MemoryStream(GetChunkBytes(aMap, ind));
                using var b = new MemoryStream(GetChunkBytes(bMap, ind));
                using var c = new MemoryStream(GetChunkBytes(cMap, ind));
                using var aR = new BinaryReader(a);
                using var bR = new BinaryReader(b);
                using var cR = new BinaryReader(c);

                var outB = new byte[ExpectedChunkSize];

                {
                    using var outS = new MemoryStream(outB);
                    using var outW = new BinaryWriter(outS);

                    for (var i = 0; i < ExpectedChunkSize; i += 4)
                    {
                        var aI = aR.ReadUInt32();
                        var bI = MapTileId(bR.ReadUInt32(), TileMapFromBasedToOurs);
                        var cI = MapTileId(cR.ReadUInt32(), TileMapFromOtherToOurs);
                        // cI needs translation.

                        uint result = aI;
                        if (aI == bI)
                        {
                            // If aI == bI then aI did not change anything, so cI always wins
                            result = cI;
                        }
                        else if (bI != cI)
                        {
                            // If bI != cI then cI definitely changed something (conflict, but overrides aI)
                            result = cI;
                            Console.WriteLine("WARNING: Tile (" + ind + ")[" + i + "] was changed by both branches.");
                        }
                        outW.Write(result);
                    }
                }

                // Actually output chunk
                if (!aMap.ContainsKey(ind))
                {
                    var res = new YamlMappingNode();
                    res.Children["ind"] = ind;
                    aMap[ind] = res;
                }
                aMap[ind].Children["tiles"] = Convert.ToBase64String(outB);
            }
        }

        public uint MapTileId(uint src, Dictionary<uint, uint> mapping)
        {
            return (src & 0xFFFF0000) | mapping[src & 0xFFFF];
        }

        public Dictionary<string, YamlMappingNode> ConvertTileChunks(YamlSequenceNode chunks)
        {
            var map = new Dictionary<string, YamlMappingNode>();
            foreach (var chunk in chunks)
                map[chunk["ind"].ToString()] = (YamlMappingNode) chunk;
            return map;
        }

        public byte[] GetChunkBytes(Dictionary<string, YamlMappingNode> chunks, string ind)
        {
            if (!chunks.ContainsKey(ind))
                return new byte[ExpectedChunkSize];
            return Convert.FromBase64String(chunks[ind]["tiles"].ToString());
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
                if (deletedByOther && !deletedByOurs)
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

        public bool MergeEntities()
        {
            bool success = true;
            foreach (var kvp in EntityMapFromOtherToOurs)
            {
                // For debug use.
                // Console.WriteLine("Entity C/" + kvp.Key + " -> A/" + kvp.Value);
                YamlMappingNode oursEnt;
                if (MapOurs.Entities.ContainsKey(kvp.Value))
                {
                    oursEnt = MapOurs.Entities[kvp.Value];
                    if (!MapBased.Entities.TryGetValue(kvp.Value, out var basedEnt))
                    {
                        basedEnt = oursEnt;
                    }

                    if (!MergeEntityNodes(oursEnt, basedEnt, MapOther.Entities[kvp.Key]))
                    {
                        Console.WriteLine("Unable to successfully merge entity C/" + kvp.Key);
                        success = false;
                    }
                }
                else
                {
                    oursEnt = (YamlMappingNode) YamlTools.CopyYamlNodes(MapOther.Entities[kvp.Key]);
                    if (!MapEntity(oursEnt)) {
                        Console.WriteLine("Unable to successfully import entity C/" + kvp.Key);
                        success = false;
                    } else {
                        MapOurs.Entities[kvp.Value] = oursEnt;
                    }
                }
                oursEnt.Children["uid"] = kvp.Value.ToString(CultureInfo.InvariantCulture);
            }
            return success;
        }

        public bool MergeEntityNodes(YamlMappingNode ours, YamlMappingNode based, YamlMappingNode other)
        {
            // Copy to intermediate
            var otherMapped = (YamlMappingNode) YamlTools.CopyYamlNodes(other);
            if (!MapEntity(otherMapped))
                return false;
            // Merge stuff that isn't components
            var path = "Entity" + other["uid"];
            YamlTools.MergeYamlMappings(ours, based, otherMapped, path, new[] {"components"});
            // Components are special
            var ourComponents = new Dictionary<string, YamlMappingNode>();
            var basedComponents = new Dictionary<string, YamlMappingNode>();
            var ourComponentsNode = (YamlSequenceNode) ours["components"];
            var basedComponentsNode = (YamlSequenceNode) based["components"];
            var otherComponentsNode = (YamlSequenceNode) otherMapped["components"];
            foreach (var component in ourComponentsNode)
            {
                var name = component["type"].ToString();
                ourComponents[name] = (YamlMappingNode) component;
            }
            foreach (var component in basedComponentsNode)
            {
                var name = component["type"].ToString();
                basedComponents[name] = (YamlMappingNode) component;
            }
            foreach (var otherComponent in otherComponentsNode)
            {
                var name = otherComponent["type"].ToString();
                if (ourComponents.ContainsKey(name))
                {
                    var ourComponent = ourComponents[name];
                    if (!basedComponents.TryGetValue(name, out var basedComponent))
                        basedComponent = new YamlMappingNode();

                    YamlTools.MergeYamlNodes(ourComponent, basedComponent, otherComponent, path + "/components/" + name);
                }
                else
                {
                    ourComponentsNode.Add(otherComponent);
                }
            }
            return true;
        }

        public bool MapEntity(YamlMappingNode other)
        {
            var path = "Entity" + other["uid"];
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
                        if (!MapEntityRecursiveAndBadly(kvp.Value, path + "/" + kvp.Key))
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
