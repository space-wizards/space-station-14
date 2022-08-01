using Content.Server.NPC.Operators;
using Content.Server.NPC.Operators.Inventory;
using Content.Server.NPC.Utility.Considerations;
using Content.Server.NPC.Utility.Considerations.Combat.Melee;
using Content.Server.NPC.Utility.Considerations.Inventory;
using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States;
using Content.Server.NPC.WorldState.States.Combat;

namespace Content.Server.NPC.Utility.Actions.Combat.Melee
{
    public sealed class EquipMelee : UtilityAction
    {
        public EntityUid Target { get; set; } = default!;

        public override void SetupOperators(Blackboard context)
        {
            ActionOperators = new Queue<AiOperator>(new AiOperator[]
            {
                new EquipEntityOperator(Owner, Target)
            });
        }

        protected override void UpdateBlackboard(Blackboard context)
        {
            base.UpdateBlackboard(context);
            context.GetState<WeaponEntityState>().SetValue(Target);
            context.GetState<TargetEntityState>().SetValue(Target);
        }

        protected override IReadOnlyCollection<Func<float>> GetConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();

            return new[]
            {
                considerationsManager.Get<CanPutTargetInInventoryCon>()
                    .BoolCurve(context),
                considerationsManager.Get<MeleeWeaponSpeedCon>()
                    .QuadraticCurve(context, 1.0f, 0.5f, 0.0f, 0.0f),
                considerationsManager.Get<MeleeWeaponDamageCon>()
                    .QuadraticCurve(context, 1.0f, 0.25f, 0.0f, 0.0f),
            };
        }
    }
}
