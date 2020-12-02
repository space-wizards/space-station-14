#nullable enable
using System.Collections.Generic;
using Content.Server.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Access
{
    /// <summary>
    ///     Simple mutable access provider found on ID cards and such.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IAccess))]
    public class AccessComponent : Component, IAccess
    {
        public override string Name => "Access";

        [ViewVariables]
        private readonly HashSet<string> _tags = new();

        public ISet<string> Tags => _tags;
        public bool IsReadOnly => false;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction("tags", new List<string>(),
                value =>
                {
                    _tags.Clear();
                    _tags.UnionWith(value);
                },
                () => new List<string>(_tags));
        }

        public void SetTags(IEnumerable<string> newTags)
        {
            _tags.Clear();
            _tags.UnionWith(newTags);
        }
    }
}
