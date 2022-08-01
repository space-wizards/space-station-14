using Content.Server.NPC.Operators;
using Content.Server.NPC.Operators.Combat.Melee;
using Content.Server.NPC.Operators.Movement;
using Content.Server.NPC.Utility.Considerations;
using Content.Server.NPC.Utility.Considerations.Combat;
using Content.Server.NPC.Utility.Considerations.Containers;
using Content.Server.NPC.Utility.Considerations.Movement;
using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States;
using Content.Server.NPC.WorldState.States.Combat;
using Content.Server.NPC.WorldState.States.Movement;
using Content.Server.Weapon.Melee.Components;

namespace Content.Server.NPC.Utility.Actions.Combat.Melee
{
    public sealed class UnarmedAttackEntity : UtilityAction
    {
        public EntityUid Target { get; set; } = default!;

        public override void SetupOperators(Blackboard context)
        {
            MoveToEntityOperator moveOperator;
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out MeleeWeaponComponent? unarmedCombatComponent))
            {
                moveOperator = new MoveToEntityOperator(Owner, Target, unarmedCombatComponent.Range - 0.01f);
            }
            // I think it's possible for this to happen given planning is time-sliced?
            // TODO: At this point we should abort
            else
            {
                moveOperator = new MoveToEntityOperator(Owner, Target);
            }

            ActionOperators = new Queue<AiOperator>(new AiOperator[]
            {
                moveOperator,
                new UnarmedCombatOperator(Owner, Target),
            });
        }

        protected override void UpdateBlackboard(Blackboard context)
        {
            base.UpdateBlackboard(context);
            context.GetState<TargetEntityState>().SetValue(Target);
            context.GetState<MoveTargetState>().SetValue(Target);
            // Can just set ourselves as entity given unarmed just inherits from meleeweapon
            context.GetState<WeaponEntityState>().SetValue(Owner);
        }

        protected override IReadOnlyCollection<Func<float>> GetConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();

            return new[]
            {
                considerationsManager.Get<TargetIsDeadCon>()
                    .InverseBoolCurve(context),
                considerationsManager.Get<TargetIsCritCon>()
                    .QuadraticCurve(context, -0.8f, 1.0f, 1.0f, 0.0f),
                considerationsManager.Get<TargetDistanceCon>()
                    .PresetCurve(context, PresetCurve.Distance),
                considerationsManager.Get<TargetHealthCon>()
                    .PresetCurve(context, PresetCurve.TargetHealth),
                considerationsManager.Get<TargetAccessibleCon>()
                    .BoolCurve(context),
                // TODO: Consider our Speed and Damage to compare this to using a weapon
                // Also need to unequip our weapon if we have one (xenos can't hold one so no issue for now)
            };
        }
    }
}
