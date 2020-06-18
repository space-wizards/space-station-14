using JetBrains.Annotations;

namespace Content.Server.AI.WorldState.States.Combat.Ranged
{
    /// <summary>
    /// How long to wait between bursts
    /// </summary>
    [UsedImplicitly]
    public sealed class BurstCooldown : PlanningStateData<float>
    {
        public override string Name => "BurstCooldown";
        public override void Reset()
        {
            Value = 0.0f;
        }
    }
}
