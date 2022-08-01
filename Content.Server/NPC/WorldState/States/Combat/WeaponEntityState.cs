using JetBrains.Annotations;

namespace Content.Server.NPC.WorldState.States.Combat
{
    [UsedImplicitly]
    public sealed class WeaponEntityState : PlanningStateData<EntityUid?>
    {
        // Similar to TargetEntity
        public override string Name => "WeaponEntity";
        public override void Reset()
        {
            Value = null;
        }
    }
}
