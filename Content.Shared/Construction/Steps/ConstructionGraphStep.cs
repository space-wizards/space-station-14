using System;
using System.Collections.Generic;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Shared.Construction.Steps
{
    [Serializable]
    [ImplicitDataDefinitionForInheritors]
    public abstract class ConstructionGraphStep
    {
        [DataField("completed", serverOnly: true)] private List<IGraphAction> _completed = new();

        [DataField("doAfter")] public float DoAfter { get; private set; }

        public IReadOnlyList<IGraphAction> Completed => _completed;

        public abstract void DoExamine(FormattedMessage message, bool inDetailsRange);
    }
}
