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
        [Dependency] private readonly IEntityManager _entMan = default!;

        private readonly float _burstTime;
        private float _elapsedTime;

        private readonly EntityUid _owner;
        private readonly EntityUid _target;

        public SwingMeleeWeaponOperator(EntityUid owner, EntityUid target, float burstTime = 1.0f)
        {
            IoCManager.InjectDependencies(this);

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

            if (!_entMan.TryGetComponent(_owner, out CombatModeComponent? combatModeComponent))
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

            if (_entMan.TryGetComponent(_owner, out CombatModeComponent? combatModeComponent))
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

            if (!_entMan.TryGetComponent(_owner, out HandsComponent? hands) || hands.GetActiveHandItem == null)
            {
                return Outcome.Failed;
            }

            var meleeWeapon = hands.GetActiveHandItem.Owner;
            _entMan.TryGetComponent(meleeWeapon, out MeleeWeaponComponent? meleeWeaponComponent);

            if ((_entMan.GetComponent<TransformComponent>(_target).Coordinates.Position - _entMan.GetComponent<TransformComponent>(_owner).Coordinates.Position).Length >
                meleeWeaponComponent?.Range)
            {
                return Outcome.Failed;
            }

            var interactionSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InteractionSystem>();

            interactionSystem.AiUseInteraction(_owner, _entMan.GetComponent<TransformComponent>(_target).Coordinates, _target);
            _elapsedTime += frameTime;
            return Outcome.Continuing;
        }
    }
}
