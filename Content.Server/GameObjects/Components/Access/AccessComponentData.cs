#nullable enable
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Access
{
    public partial class AccessComponentData
    {
        [DataClassTarget("tags")]
        private HashSet<string>? _tags;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

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
