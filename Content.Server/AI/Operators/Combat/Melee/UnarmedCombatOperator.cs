using Content.Server.CombatMode;
using Content.Server.Interaction;
using Content.Server.Weapon.Melee.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Operators.Combat.Melee
{
    public sealed class UnarmedCombatOperator : AiOperator
    {
        private readonly float _burstTime;
        private float _elapsedTime;

        private readonly IEntity _owner;
        private readonly IEntity _target;
        private UnarmedCombatComponent? _unarmedCombat;

        public UnarmedCombatOperator(IEntity owner, IEntity target, float burstTime = 1.0f)
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

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(_owner, out CombatModeComponent? combatModeComponent))
            {
                return false;
            }

            if (!combatModeComponent.IsInCombatMode)
            {
                combatModeComponent.IsInCombatMode = true;
            }

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(_owner, out UnarmedCombatComponent? unarmedCombatComponent))
            {
                _unarmedCombat = unarmedCombatComponent;
            }
            else
            {
                return false;
            }

            return true;
        }

        public override bool Shutdown(Outcome outcome)
        {
            if (!base.Shutdown(outcome))
                return false;

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(_owner, out CombatModeComponent? combatModeComponent))
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

            if (_unarmedCombat?.Deleted ?? true)
            {
                return Outcome.Failed;
            }

            if ((IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(_target).Coordinates.Position - IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(_owner).Coordinates.Position).Length >
                _unarmedCombat.Range)
            {
                return Outcome.Failed;
            }

            var interactionSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InteractionSystem>();
            interactionSystem.AiUseInteraction(_owner, IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(_target).Coordinates, _target);
            _elapsedTime += frameTime;
            return Outcome.Continuing;
        }
    }
}
