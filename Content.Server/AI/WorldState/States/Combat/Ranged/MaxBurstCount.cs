using JetBrains.Annotations;

namespace Content.Server.AI.WorldState.States.Combat.Ranged
{
    /// <summary>
    /// How many shots to take before cooling down
    /// </summary>
    [UsedImplicitly]
    public sealed class MaxBurstCount : PlanningStateData<int>
    {
        public override string Name => "BurstCount";
        public override void Reset()
        {
            Value = 0;
        }
    }
}
