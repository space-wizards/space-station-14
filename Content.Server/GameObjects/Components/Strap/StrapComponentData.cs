using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Strap
{
    public partial class StrapComponentData
    {
        [CustomYamlField("size")]
        public int Size;

        [CustomYamlField("list")]
        public HashSet<IEntity> BuckledEntities;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            var defaultSize = 100;

            serializer.DataField(ref Size, "size", defaultSize);
            BuckledEntities = new HashSet<IEntity>(Size / defaultSize);
        }
    }
}
