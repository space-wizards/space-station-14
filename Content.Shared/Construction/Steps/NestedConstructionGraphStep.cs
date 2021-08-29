using System.Collections.Generic;
using System.IO;
using System.Linq;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public class NestedConstructionGraphStep : ConstructionGraphStep, ISerializationHooks
    {
        [DataField("steps")] public List<List<ConstructionGraphStep>> Steps { get; private set; } = new();

        void ISerializationHooks.AfterDeserialization()
        {
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
