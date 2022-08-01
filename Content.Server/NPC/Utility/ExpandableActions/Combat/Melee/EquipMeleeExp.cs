using Content.Server.NPC.Utility.Actions;
using Content.Server.NPC.Utility.Actions.Combat.Melee;
using Content.Server.NPC.Utility.Considerations;
using Content.Server.NPC.Utility.Considerations.Combat.Melee;
using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States;
using Content.Server.NPC.WorldState.States.Inventory;
using Content.Server.Weapon.Melee.Components;

namespace Content.Server.NPC.Utility.ExpandableActions.Combat.Melee
{
    public sealed class EquipMeleeExp : ExpandableUtilityAction
    {
        public override float Bonus => UtilityAction.CombatPrepBonus;

        protected override IEnumerable<Func<float>> GetCommonConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();

            return new[]
            {
                considerationsManager.Get<MeleeWeaponEquippedCon>()
                    .InverseBoolCurve(context),
            };
        }

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            foreach (var entity in context.GetState<EnumerableInventoryState>().GetValue())
            {
                if (!IoCManager.Resolve<IEntityManager>().HasComponent<MeleeWeaponComponent>(entity))
                {
                    continue;
                }

                yield return new EquipMelee {Owner = owner, Target = entity, Bonus = Bonus};
            }
        }
    }
}
