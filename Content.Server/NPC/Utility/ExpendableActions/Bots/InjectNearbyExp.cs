using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Server.NPC.Utility.Actions;
using Content.Server.NPC.Utility.Actions.Bots;
using Content.Server.NPC.Utility.Considerations;
using Content.Server.NPC.Utility.Considerations.ActionBlocker;
using Content.Server.NPC.Utility.ExpandableActions;
using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States;
using Content.Server.Silicons.Bots;

namespace Content.Server.NPC.Utility.ExpendableActions.Bots
{
    public sealed class InjectNearbyExp : ExpandableUtilityAction
    {
        public override float Bonus => 30;
        IEntityManager _entMan = IoCManager.Resolve<IEntityManager>();

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
            if (!_entMan.TryGetComponent(owner, out NPCComponent? controller)
                || !_entMan.TryGetComponent(owner, out MedibotComponent? bot))
            {
                throw new InvalidOperationException();
            }

            yield return new InjectNearby() {Owner = owner, Target = EntitySystem.Get<InjectNearbySystem>().GetNearbyInjectable(Owner), Bonus = Bonus};
        }
    }
}
