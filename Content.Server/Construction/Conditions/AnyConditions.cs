using System;
using System.Threading.Tasks;
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

        public async Task<bool> Condition(IEntity entity)
        {
            foreach (var condition in Conditions)
            {
                if (await condition.Condition(entity))
                    return true;
            }

            return false;
        }
    }
}
