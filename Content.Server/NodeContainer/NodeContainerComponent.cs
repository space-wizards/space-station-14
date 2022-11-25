using System.Diagnostics.CodeAnalysis;
using Content.Server.NodeContainer.Nodes;

namespace Content.Server.NodeContainer
{
    /// <summary>
    ///     Creates and maintains a set of <see cref="Node"/>s.
    /// </summary>
    [RegisterComponent]
    public sealed class NodeContainerComponent : Component
    {
        //HACK: THIS BEING readOnly IS A FILTHY HACK AND I HATE IT --moony
        [DataField("nodes", readOnly: true)] public Dictionary<string, Node> Nodes { get; } = new();

        [DataField("examinable")] public bool Examinable = false;

        public T GetNode<T>(string identifier) where T : Node
        {
            return (T) Nodes[identifier];
        }

        public bool TryGetNode<T>(string identifier, [NotNullWhen(true)] out T? node) where T : Node
        {
            if (Nodes.TryGetValue(identifier, out var n) && n is T t)
            {
                node = t;
                return true;
            }

            node = null;
            return false;
        }
    }
}
