using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Content.Tools
{
    public static class YamlTools
    {
        public static YamlNode CopyYamlNodes(YamlNode other)
        {
            switch (other)
            {
                case YamlSequenceNode subSequence:
                    YamlSequenceNode tmp1 = new YamlSequenceNode();
                    MergeYamlSequences((YamlSequenceNode) tmp1, new YamlSequenceNode(), (YamlSequenceNode) other, "");
                    return tmp1;
                case YamlMappingNode subMapping:
                    YamlMappingNode tmp2 = new YamlMappingNode();
                    MergeYamlMappings((YamlMappingNode) tmp2, new YamlMappingNode(), (YamlMappingNode) other, "");
                    return tmp2;
                case YamlScalarNode subScalar:
                    YamlScalarNode tmp3 = new YamlScalarNode();
                    CopyYamlScalar(tmp3, subScalar);
                    return tmp3;
                default:
                    throw new ArgumentException($"Unrecognized YAML node type for copy: {other.GetType()}", nameof(other));
            }
        }

        public static bool TriTypeMatch(YamlNode ours, YamlNode based, YamlNode other)
        {
            var refType = other.GetType();
            if (refType != based.GetType())
                return false;
            if (refType != ours.GetType())
                return false;
            return true;
        }

        public static void MergeYamlNodes(YamlNode ours, YamlNode based, YamlNode other, string path)
        {
            if (!TriTypeMatch(ours, based, other))
                throw new ArgumentException($"Node type mismatch at {path}");
            switch (other)
            {
                case YamlSequenceNode subSequence:
                    MergeYamlSequences((YamlSequenceNode) ours, (YamlSequenceNode) based, (YamlSequenceNode) other, path);
                    break;
                case YamlMappingNode subMapping:
                    MergeYamlMappings((YamlMappingNode) ours, (YamlMappingNode) based, (YamlMappingNode) other, path);
                    break;
                case YamlScalarNode subScalar:
                    CopyYamlScalar((YamlScalarNode) ours, (YamlScalarNode) based);
                    break;
                default:
                    throw new ArgumentException($"Unrecognized YAML node type at {path}: {other.GetType()}", nameof(other));
            }
        }

        public static void MergeYamlSequences(YamlSequenceNode ours, YamlSequenceNode based, YamlSequenceNode other, string path)
        {
            // for now, just copy other -> ours
            // I am aware this is terrible
            ours.Children.Clear();
            foreach (var c in other.Children)
                ours.Add(CopyYamlNodes(c));
        }

        public static void MergeYamlMappings(YamlMappingNode ours, YamlMappingNode based, YamlMappingNode other, string path)
        {
            // Deletions/modifications
            foreach (var kvp in based)
            {
                var deletedByOurs = !ours.Children.ContainsKey(kvp.Key);
                var deletedByOther = !other.Children.ContainsKey(kvp.Key);
                if (deletedByOther && (!deletedByOurs))
                {
                    // Delete
                    ours.Children.Remove(kvp.Key);
                }
                else if (!(deletedByOurs || deletedByOther))
                {
                    // Modify
                    var a = ours[kvp.Key];
                    var b = kvp.Value; // based[kvp.Key]
                    var c = other[kvp.Key];
                    if (!TriTypeMatch(a, b, c))
                    {
                        ours.Children[kvp.Key] = CopyYamlNodes(c);
                    }
                    else
                    {
                        MergeYamlNodes(a, b, c, path);
                    }
                }
            }
            // Additions
            foreach (var kvp in other)
            {
                if (!based.Children.ContainsKey(kvp.Key))
                {
                    if (ours.Children.ContainsKey(kvp.Key))
                    {
                        // Both sides added the same key. Try to merge.
                        var a = ours[kvp.Key];
                        var b = based[kvp.Key];
                        var c = kvp.Value; // other[kvp.Key]
                        if (!TriTypeMatch(a, b, c))
                        {
                            ours.Children[kvp.Key] = CopyYamlNodes(c);
                        }
                        else
                        {
                            MergeYamlNodes(a, b, c, path);
                        }
                    }
                    else
                    {
                        // Well that was easy
                        ours.Children[kvp.Key] = CopyYamlNodes(kvp.Value);
                    }
                }
            }
        }

        // NOTE: This is a heuristic ONLY! And is also not used at the moment because sequence matching isn't in place.
        // It could also be massively improved.
        public static float YamlNodesHeuristic(YamlNode a, YamlNode b)
        {
            if (a.GetType() != b.GetType())
                return 0.0f;
            switch (a)
            {
                case YamlSequenceNode x:
                    return YamlSequencesHeuristic((YamlSequenceNode) a, (YamlSequenceNode) b);
                case YamlMappingNode y:
                    return YamlMappingsHeuristic((YamlMappingNode) a, (YamlMappingNode) b);
                case YamlScalarNode z:
                    return (((YamlScalarNode) a).Value == ((YamlScalarNode) b).Value) ? 1.0f : 0.0f;
                default:
                    throw new ArgumentException($"Unrecognized YAML node type: {a.GetType()}", nameof(a));
            }
        }

        public static float YamlSequencesHeuristic(YamlSequenceNode a, YamlSequenceNode b)
        {
            if (a.Children.Count != b.Children.Count)
                return 0.0f;
            if (a.Children.Count == 0)
                return 1.0f;
            var total = 0.0f;
            for (var i = 0; i < a.Children.Count; i++)
                total += YamlNodesHeuristic(a.Children[i], b.Children[i]);
            return total / a.Children.Count;
        }

        public static float YamlMappingsHeuristic(YamlMappingNode a, YamlMappingNode b)
        {
            return (a == b) ? 1.0f : 0.0f;
        }

        public static void CopyYamlScalar(YamlScalarNode dst, YamlScalarNode src)
        {
            dst.Value = src.Value;
            dst.Style = src.Style;
        }
    }
}
