using System;
using System.Linq;
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
                    MergeYamlSequences(tmp1, new YamlSequenceNode(), subSequence, "");
                    return tmp1;
                case YamlMappingNode subMapping:
                    YamlMappingNode tmp2 = new YamlMappingNode();
                    MergeYamlMappings(tmp2, new YamlMappingNode(), subMapping, "", new string[] {});
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
                    MergeYamlSequences((YamlSequenceNode) ours, (YamlSequenceNode) based, subSequence, path);
                    break;
                case YamlMappingNode subMapping:
                    MergeYamlMappings((YamlMappingNode) ours, (YamlMappingNode) based, subMapping, path, new string[] {});
                    break;
                case YamlScalarNode subScalar:
                    // Console.WriteLine(path + " - " + ours + " || " + based + " || " + other);
                    var scalarA = (YamlScalarNode) ours;
                    var scalarB = (YamlScalarNode) based;
                    var scalarC = subScalar;
                    var aeb = (scalarA.Value == scalarB.Value);
                    var cneb = (scalarC.Value != scalarB.Value);
                    if (aeb || cneb)
                        CopyYamlScalar(scalarA, scalarC);
                    // Console.WriteLine(path + " . " + ours + " || " + based + " || " + other);
                    break;
                default:
                    throw new ArgumentException($"Unrecognized YAML node type at {path}: {other.GetType()}", nameof(other));
            }
        }

        public static void MergeYamlSequences(YamlSequenceNode ours, YamlSequenceNode based, YamlSequenceNode other, string path)
        {
            if ((ours.Children.Count == based.Children.Count) && (other.Children.Count == ours.Children.Count))
            {
                // this is terrible and doesn't do proper rearrange detection
                // but it looks as if vectors might be arrays
                // so rearrange detection might break more stuff...
                // nope, they aren't, but still good to have
                for (var i = 0; i < ours.Children.Count; i++)
                    MergeYamlNodes(ours.Children[i], based.Children[i], other.Children[i], path + "/" + i);
                return;
            }
            // for now, just copy other -> ours
            // I am aware this is terrible
            ours.Children.Clear();
            foreach (var c in other.Children)
                ours.Add(CopyYamlNodes(c));
        }

        public static void MergeYamlMappings(YamlMappingNode ours, YamlMappingNode based, YamlMappingNode other, string path, string[] ignoreThese)
        {
            // Deletions/modifications
            foreach (var kvp in based)
            {
                if (ignoreThese.Contains(kvp.Key.ToString()))
                    continue;

                var localPath = path + "/" + kvp.Key;
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
                        Console.WriteLine("Warning: Type mismatch (defaulting to value C) at " + localPath);
                        ours.Children[kvp.Key] = CopyYamlNodes(c);
                    }
                    else
                    {
                        MergeYamlNodes(a, b, c, localPath);
                    }
                }
            }
            // Additions
            foreach (var kvp in other)
            {
                if (ignoreThese.Contains(kvp.Key.ToString()))
                    continue;

                var localPath = path + "/" + kvp.Key;
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
                            Console.WriteLine("Warning: Type mismatch (defaulting to value C) at " + localPath);
                            ours.Children[kvp.Key] = CopyYamlNodes(c);
                        }
                        else
                        {
                            MergeYamlNodes(a, b, c, localPath);
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
                    return YamlSequencesHeuristic(x, (YamlSequenceNode) b);
                case YamlMappingNode y:
                    return YamlMappingsHeuristic(y, (YamlMappingNode) b);
                case YamlScalarNode z:
                    return (z.Value == ((YamlScalarNode) b).Value) ? 1.0f : 0.0f;
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
            return Equals(a, b) ? 1.0f : 0.0f;
        }

        public static void CopyYamlScalar(YamlScalarNode dst, YamlScalarNode src)
        {
            dst.Value = src.Value;
            dst.Style = src.Style;
        }
    }
}
