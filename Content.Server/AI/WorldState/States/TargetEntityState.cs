using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.WorldState.States
{
    /// <summary>
    /// Could be target item to equip, target to attack, etc.
    /// </summary>
    [UsedImplicitly]
    public sealed class TargetEntityState : PlanningStateData<EntityUid?>
    {
        public override string Name => "TargetEntity";

        public override void Reset()
        {
            Value = null;
        }
    }
}
