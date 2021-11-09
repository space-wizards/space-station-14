using System;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public class DumpCanisterBehavior : IThresholdBehavior
    {
        public void Execute(EntityUid owner, DestructibleSystem system, IEntityManager entityManager)
        {
            var gasCanisterSystem = entityManager.EntitySysManager.GetEntitySystem<GasCanisterSystem>();

            gasCanisterSystem.PurgeContents(owner);
        }
    }
}
