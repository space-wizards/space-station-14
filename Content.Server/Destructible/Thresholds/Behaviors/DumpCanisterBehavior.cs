using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Behaviors;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public sealed partial class DumpCanisterBehavior : IThresholdBehavior
    {
        public void Execute(EntityUid owner, DestructibleBehaviorSystem system, EntityUid? cause = null)
        {
            system.EntityManager.EntitySysManager.GetEntitySystem<GasCanisterSystem>().PurgeContents(owner);
        }
    }
}
