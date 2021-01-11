using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Weapon.Melee;
using Content.Server.GameObjects.EntitySystems.Click;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Operators.Combat.Melee
{
    public sealed class UnarmedCombatOperator : AiOperator
    {
        private readonly float _burstTime;
        private float _elapsedTime;

        private readonly IEntity _owner;
        private readonly IEntity _target;
        private UnarmedCombatComponent _unarmedCombat;

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

            if (!_owner.TryGetComponent(out CombatModeComponent combatModeComponent))
            {
                return false;
            }

            if (!combatModeComponent.IsInCombatMode)
            {
                combatModeComponent.IsInCombatMode = true;
            }

            if (_owner.TryGetComponent(out UnarmedCombatComponent unarmedCombatComponent))
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

            if (_owner.TryGetComponent(out CombatModeComponent combatModeComponent))
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

            if (_unarmedCombat.Deleted)
            {
                return Outcome.Failed;
            }

            if ((_target.Transform.Coordinates.Position - _owner.Transform.Coordinates.Position).Length >
                _unarmedCombat.Range)
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
