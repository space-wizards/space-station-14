using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Access
{
    public partial class AccessReaderComponentData
    {
        [CustomYamlField("accessList")]
        public List<ISet<string>> AccessLists = new();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction("access", new List<List<string>>(),
                v =>
                {
                    if (v.Count != 0)
                    {
                        AccessLists.Clear();
                        AccessLists.AddRange(v.Select(a => new HashSet<string>(a)));
                    }
                },
                () => AccessLists.Select(p => new List<string>(p)).ToList());
        }
    }
}
