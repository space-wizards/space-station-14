using System;
using System.Collections.Generic;
using Content.Shared.Interfaces;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Construction
{
    [Serializable]
    public abstract class ConstructionGraphStep : IExposeData
    {
        private List<IGraphAction> _completed;
        public float DoAfter { get; private set; }
        public IReadOnlyList<IGraphAction> Completed => _completed;

        public virtual void ExposeData(ObjectSerializer serializer)
        {
            var moduleManager = IoCManager.Resolve<IModuleManager>();

            serializer.DataField(this, x => x.DoAfter, "doAfter", 0f);
            if (!moduleManager.IsServerModule) return;
            serializer.DataField(ref _completed, "completed", new List<IGraphAction>());
        }

        public abstract void DoExamine(FormattedMessage message, bool inDetailsRange);
    }
}
