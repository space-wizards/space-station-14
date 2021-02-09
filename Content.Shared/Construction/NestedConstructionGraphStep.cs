using System.Collections.Generic;
using System.IO;
using System.Linq;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Construction
{
    public class NestedConstructionGraphStep : ConstructionGraphStep
    {
        public List<List<ConstructionGraphStep>> Steps { get; private set; } = new();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => x.Steps, "steps", new());

            if (Steps.Any(inner => inner.Any(step => step is NestedConstructionGraphStep)))
            {
                throw new InvalidDataException("Can't have nested construction steps inside nested construction steps!");
            }
        }

        public override void DoExamine(FormattedMessage message, bool inDetailsRange)
        {
        }
    }
}
