#nullable enable
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Access
{
    public partial class AccessComponentData
    {
        [DataClassTarget("tags")]
        private HashSet<string>? _tags;

        public void ExposeData(ObjectSerializer serializer)
        {
            _tags ??= new HashSet<string>();
            serializer.DataReadWriteFunction("tags", new List<string>(),
                value =>
                {
                    _tags.Clear();
                    _tags.UnionWith(value);
                },
                () => new List<string>(_tags));
            if (_tags.Count == 0) _tags = null;
        }
    }
}
