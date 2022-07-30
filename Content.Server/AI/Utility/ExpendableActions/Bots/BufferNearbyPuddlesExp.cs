using Content.Server.AI.Components;
using Content.Server.AI.EntitySystems;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Bots;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Combat.Melee;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.Utility.Considerations.ActionBlocker;



namespace Content.Server.AI.Utility.ExpandableActions.Bots
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
