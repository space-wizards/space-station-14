using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Access
{
    public partial class AccessComponentData
    {
        [CustomYamlField("tags")]
        private readonly HashSet<string> _tags = new();

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
    }
}
