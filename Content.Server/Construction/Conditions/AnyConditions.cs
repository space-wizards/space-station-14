using System;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public class AnyConditions : IGraphCondition
    {
        [DataField("conditions")]
        public IGraphCondition[] Conditions { get; } = Array.Empty<IGraphCondition>();

        public bool Condition(EntityUid uid, IEntityManager entityManager)
        {
            foreach (var condition in Conditions)
            {
                if (condition.Condition(uid, entityManager))
                    return true;
            }

            return false;
        }

        // TODO CONSTRUCTION: Examine for this condition.
    }
}
