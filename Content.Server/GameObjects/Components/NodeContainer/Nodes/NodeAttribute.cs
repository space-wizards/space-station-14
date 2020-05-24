using System;

namespace Content.Server.GameObjects.Components.NodeContainer.Nodes
{
    /// <summary>
    ///     Associates a <see cref="INode"/> implementation with a string. This is used
    ///     to specify an <see cref="INode"/>'s strategy in yaml.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class
        NodeAttribute : Attribute
    {
        public string Name { get; }

        public NodeAttribute(string name)
        {
            Name = name;
        }
    }
}
