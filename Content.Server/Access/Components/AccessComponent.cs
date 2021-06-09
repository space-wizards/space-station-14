#nullable enable
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Access.Components
{
    /// <summary>
    ///     Simple mutable access provider found on ID cards and such.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IAccess))]
    public class AccessComponent : Component, IAccess
    {
        public override string Name => "Access";

        [DataField("tags")]
        [ViewVariables]
        private readonly HashSet<string> _tags = new();

        public ISet<string> Tags => _tags;
        public bool IsReadOnly => false;

        public void SetTags(IEnumerable<string> newTags)
        {
            _tags.Clear();
            _tags.UnionWith(newTags);
        }
    }
}
