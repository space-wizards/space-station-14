using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.Weapon.Melee.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.Considerations.Combat.Melee
{
    public sealed class MeleeWeaponEquippedCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            return IoCManager.Resolve<IEntityManager>()
                .HasComponent<MeleeWeaponComponent>(context.GetState<EquippedEntityState>().GetValue())
                ? 1.0f
                : 0.0f;
        }
    }
}
