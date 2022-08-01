using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Server.NPC.Utility.Actions;
using Content.Server.NPC.Utility.Actions.Combat.Melee;
using Content.Server.NPC.Utility.Considerations;
using Content.Server.NPC.Utility.Considerations.Combat.Melee;
using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States;

namespace Content.Server.NPC.Utility.ExpandableActions.Combat.Melee
{
    public sealed class UnarmedAttackNearbyHostilesExp : ExpandableUtilityAction
    {
        public override float Bonus => UtilityAction.CombatBonus;

        protected override IEnumerable<Func<float>> GetCommonConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();

            return new[]
            {
                considerationsManager.Get<CanUnarmedCombatCon>()
                    .BoolCurve(context),
            };
        }

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(owner, out NPCComponent? controller))
            {
                throw new InvalidOperationException();
            }

            foreach (var target in EntitySystem.Get<AiFactionTagSystem>()
                .GetNearbyHostiles(owner, controller.VisionRadius))
            {
                yield return new UnarmedAttackEntity() {Owner = owner, Target = target, Bonus = Bonus};
            }
        }
    }
}
