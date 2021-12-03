using Content.Server.CombatMode;
using Content.Server.Hands.Components;
using Content.Server.Interaction;
using Content.Server.Weapon.Melee.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Operators.Combat.Melee
{
    public class SwingMeleeWeaponOperator : AiOperator
    {
        private readonly float _burstTime;
        private float _elapsedTime;

        private readonly IEntity _owner;
        private readonly IEntity _target;

        public SwingMeleeWeaponOperator(IEntity owner, IEntity target, float burstTime = 1.0f)
        {
            _owner = owner;
            _target = target;
            _burstTime = burstTime;
        }

        public override bool Startup()
        {
            if (!base.Startup())
            {
                return true;
            }

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(_owner.Uid, out CombatModeComponent? combatModeComponent))
            {
                return false;
            }

            if (!combatModeComponent.IsInCombatMode)
            {
                combatModeComponent.IsInCombatMode = true;
            }

            return true;
        }

        public override bool Shutdown(Outcome outcome)
        {
            if (!base.Shutdown(outcome))
                return false;

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(_owner.Uid, out CombatModeComponent? combatModeComponent))
            {
                combatModeComponent.IsInCombatMode = false;
            }

            return true;
        }

        public override Outcome Execute(float frameTime)
        {
            if (_burstTime <= _elapsedTime)
            {
                return Outcome.Success;
            }

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(_owner.Uid, out HandsComponent? hands) || hands.GetActiveHand == null)
            {
                return Outcome.Failed;
            }

            var meleeWeapon = hands.GetActiveHand.Owner;
            IoCManager.Resolve<IEntityManager>().TryGetComponent(meleeWeapon.Uid, out MeleeWeaponComponent? meleeWeaponComponent);

            if ((IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(_target.Uid).Coordinates.Position - IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(_owner.Uid).Coordinates.Position).Length >
                meleeWeaponComponent?.Range)
            {
                return Outcome.Failed;
            }

            var interactionSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InteractionSystem>();

            interactionSystem.AiUseInteraction(_owner, IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(_target.Uid).Coordinates, _target.Uid);
            _elapsedTime += frameTime;
            return Outcome.Continuing;
        }
    }
}
