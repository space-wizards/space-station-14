using Content.Server.NPC.Utility.Actions;
using Content.Server.NPC.Utility.Actions.Combat.Melee;
using Content.Server.NPC.Utility.Considerations;
using Content.Server.NPC.Utility.Considerations.Combat.Melee;
using Content.Server.NPC.Utility.Considerations.Hands;
using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States;
using Content.Server.NPC.WorldState.States.Combat.Nearby;

namespace Content.Server.NPC.Utility.ExpandableActions.Combat.Melee
{
    public sealed class PickUpMeleeWeaponExp : ExpandableUtilityAction
    {
        public override float Bonus => UtilityAction.CombatPrepBonus;

        protected override IEnumerable<Func<float>> GetCommonConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();

            return new[]
            {
                considerationsManager.Get<FreeHandCon>()
                    .BoolCurve(context),
                considerationsManager.Get<HasMeleeWeaponCon>()
                    .InverseBoolCurve(context),
            };
        }

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            foreach (var entity in context.GetState<NearbyMeleeWeapons>().GetValue())
            {
                yield return new PickUpMeleeWeapon() {Owner = owner, Target = entity, Bonus = Bonus};
            }
        }
    }
}
