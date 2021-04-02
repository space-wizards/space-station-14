using System;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Chemistry
{
    public enum ReactionMethod
    {
        Touch,
        Injection,
        Ingestion,
    }

    [DataDefinition]
    public abstract class ReagentEntityReaction
    {
        [ViewVariables]
        [field: DataField("touch")]
        public bool Touch { get; } = false;

        [ViewVariables]
        [field: DataField("injection")]
        public bool Injection { get; } = false;

        [ViewVariables]
        [field: DataField("ingestion")]
        public bool Ingestion { get; } = false;

        public void React(ReactionMethod method, IEntity entity, ReagentPrototype reagent, ReagentUnit volume, Solution? source)
        {
            switch (method)
            {
                case ReactionMethod.Touch:
                    if (!Touch)
                        return;
                    break;
                case ReactionMethod.Injection:
                    if(!Injection)
                        return;
                    break;
                case ReactionMethod.Ingestion:
                    if(!Ingestion)
                        return;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(method), method, null);
            }

            React(entity, reagent, volume, source);
        }

        protected abstract void React(IEntity entity, ReagentPrototype reagent, ReagentUnit volume, Solution? source);
    }
}
