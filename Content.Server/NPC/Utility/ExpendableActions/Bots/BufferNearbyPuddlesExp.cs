using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Server.NPC.Utility.Actions;
using Content.Server.NPC.Utility.Actions.Bots;
using Content.Server.NPC.Utility.Considerations;
using Content.Server.NPC.Utility.Considerations.ActionBlocker;
using Content.Server.NPC.Utility.ExpandableActions;
using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States;

namespace Content.Server.NPC.Utility.ExpendableActions.Bots
{
    public sealed class BufferNearbyPuddlesExp : ExpandableUtilityAction
    {
        public override float Bonus => 30;

        protected override IReadOnlyCollection<Func<float>> GetCommonConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();

            return new[]
            {
                considerationsManager.Get<CanMoveCon>()
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

            yield return new GoToPuddleAndWait() {Owner = owner, Target = EntitySystem.Get<GoToPuddleSystem>().GetNearbyPuddle(Owner), Bonus = Bonus};
        }
    }
}
