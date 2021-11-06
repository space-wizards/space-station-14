using System;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public class DumpCanisterBehavior: IThresholdBehavior
    {
        public void Execute(IEntity owner, DestructibleSystem system)
        {
            var gasCanisterSystem = EntitySystem.Get<GasCanisterSystem>();

            gasCanisterSystem.PurgeContents(owner.Uid);
        }
    }
}
