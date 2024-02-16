using Content.Server.Atmos.Piping.Unary.EntitySystems;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public sealed partial class DumpCanisterBehavior : IThresholdBehavior
    {
        public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
        {
            system.EntityManager.EntitySysManager.GetEntitySystem<GasCanisterSystem>().PurgeContents(owner);
        }
    }
}
