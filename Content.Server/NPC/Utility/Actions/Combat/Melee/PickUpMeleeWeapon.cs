using Content.Server.NPC.Operators.Sequences;
using Content.Server.NPC.Utility.Considerations;
using Content.Server.NPC.Utility.Considerations.Combat.Melee;
using Content.Server.NPC.Utility.Considerations.Containers;
using Content.Server.NPC.Utility.Considerations.Movement;
using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States;
using Content.Server.NPC.WorldState.States.Combat;

namespace Content.Server.NPC.Utility.Actions.Combat.Melee
{
    public sealed class PickUpMeleeWeapon : UtilityAction
    {
        public EntityUid Target { get; set; } = default!;

        public override void SetupOperators(Blackboard context)
        {
            ActionOperators = new GoPickupEntitySequence(Owner, Target).Sequence;
        }

        protected override void UpdateBlackboard(Blackboard context)
        {
            base.UpdateBlackboard(context);
            context.GetState<TargetEntityState>().SetValue(Target);
            context.GetState<WeaponEntityState>().SetValue(Target);
        }

        protected override IReadOnlyCollection<Func<float>> GetConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();

            return new[]
            {
                considerationsManager.Get<TargetDistanceCon>()
                    .PresetCurve(context, PresetCurve.Distance),
                considerationsManager.Get<MeleeWeaponDamageCon>()
                    .QuadraticCurve(context, 1.0f, 0.25f, 0.0f, 0.0f),
                considerationsManager.Get<MeleeWeaponSpeedCon>()
                    .QuadraticCurve(context, -1.0f, 0.5f, 1.0f, 0.0f),
                considerationsManager.Get<TargetAccessibleCon>()
                    .BoolCurve(context),
            };
        }
    }
}
