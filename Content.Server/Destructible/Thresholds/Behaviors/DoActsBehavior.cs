using Content.Shared.Destructible.Thresholds;
using Content.Shared.Destructible.Thresholds.Behaviors;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public sealed partial class DoActsBehavior : IThresholdBehavior
    {
        /// <summary>
        ///     What acts should be triggered upon activation.
        /// </summary>
        [DataField]
        public ThresholdActs Acts;

        public bool HasAct(ThresholdActs act)
        {
            return (Acts & act) != 0;
        }

        public void Execute(EntityUid owner,
            IDependencyCollection collection,
            EntityManager entManager,
            EntityUid? cause = null)
        {
            var system = entManager.System<DestructibleSystem>();

            if (HasAct(ThresholdActs.Breakage))
            {
                system.BreakEntity(owner);
            }

            if (HasAct(ThresholdActs.Destruction))
            {
                system.DestroyEntity(owner);
            }
        }
    }
}
