using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Shared.Destructible.Thresholds.Behaviors;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public sealed partial class DumpCanisterBehavior : IThresholdBehavior
    {
        public void Execute(EntityUid owner,
            IDependencyCollection collection,
            EntityManager entManager,
            EntityUid? cause = null)
        {
            entManager.System<GasCanisterSystem>().PurgeContents(owner);
        }
    }
}
