#nullable enable
using System.Collections.Generic;
using Content.Server.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Access
{
    /// <summary>
    ///     Simple mutable access provider found on ID cards and such.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IAccess))]
    [CustomDataClass(typeof(AccessComponentData))]
    public class AccessComponent : Component, IAccess
    {
        public override string Name => "Access";

        [ViewVariables]
        [DataClassTarget("tags")]
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
