using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Weapon.Melee;
using Content.Server.GameObjects.EntitySystems.Click;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Operators.Combat.Melee
{
    public class SwingMeleeWeaponOperator : AiOperator
    {
        private float _burstTime;
        private float _elapsedTime;

        private readonly IEntity _owner;
        private readonly IEntity _target;

        public SwingMeleeWeaponOperator(IEntity owner, IEntity target, float burstTime = 1.0f)
        {
            _owner = owner;
            _target = target;
            _burstTime = burstTime;
        }

        public override bool TryStartup()
        {
            if (!base.TryStartup())
            {
                return true;
            }

            if (!_owner.TryGetComponent(out CombatModeComponent combatModeComponent))
            {
                return false;
            }

            if (!combatModeComponent.IsInCombatMode)
            {
                combatModeComponent.IsInCombatMode = true;
            }

            return true;
        }

        public override void Shutdown(Outcome outcome)
        {
            base.Shutdown(outcome);
            if (_owner.TryGetComponent(out CombatModeComponent combatModeComponent))
            {
                combatModeComponent.IsInCombatMode = false;
            }
        }

        public override Outcome Execute(float frameTime)
        {
            if (_burstTime <= _elapsedTime)
            {
                return Outcome.Success;
            }

            if (!_owner.TryGetComponent(out HandsComponent hands) || hands.GetActiveHand == null)
            {
                return Outcome.Failed;
            }

            var meleeWeapon = hands.GetActiveHand.Owner;
            meleeWeapon.TryGetComponent(out MeleeWeaponComponent meleeWeaponComponent);

            if ((_target.Transform.Coordinates.Position - _owner.Transform.Coordinates.Position).Length >
                meleeWeaponComponent.Range)
            {
                return Outcome.Failed;
            }

            var interactionSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InteractionSystem>();

            interactionSystem.UseItemInHand(_owner, _target.Transform.Coordinates, _target.Uid);
            _elapsedTime += frameTime;
            return Outcome.Continuing;
        }
    }
}
