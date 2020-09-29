using System.Collections.Generic;
using Robust.Shared.Serialization;

namespace Content.Shared.Construction
{
    public class NestedConstructionGraphStep : ConstructionGraphStep
    {
        public List<List<ConstructionGraphStep>> Steps { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Steps, "steps", new List<List<ConstructionGraphStep>>());
        }
    }
}
