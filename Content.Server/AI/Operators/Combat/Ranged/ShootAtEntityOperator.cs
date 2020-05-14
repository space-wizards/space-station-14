using Content.Server.GameObjects;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.GameObjects.Components.Weapon.Ranged;
using Robust.Shared.Interfaces.GameObjects;


namespace Content.Server.AI.Operators.Combat.Ranged
{
    public class ShootAtEntityOperator : AiOperator
    {
        private IEntity _owner;
        private IEntity _target;
        private float _accuracy;

        private float _burstTime;

        private float _elapsedTime;

        public ShootAtEntityOperator(IEntity owner, IEntity target, float accuracy, float burstTime = 0.5f)
        {
            _owner = owner;
            _target = target;
            _accuracy = accuracy;
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
            // TODO: Probably just do all the checks on first try and then after that repeat the fire.
            if (_burstTime <= _elapsedTime)
            {
                return Outcome.Success;
            }

            _elapsedTime += frameTime;

            if (_target.TryGetComponent(out DamageableComponent damageableComponent))
            {
                if (damageableComponent.IsDead())
                {
                    return Outcome.Success;
                }
            }
            
            if (!_owner.TryGetComponent(out HandsComponent hands) || hands.GetActiveHand == null)
            {
                return Outcome.Failed;
            }

            var equippedWeapon = hands.GetActiveHand.Owner;

            if ((_target.Transform.GridPosition.Position - _owner.Transform.GridPosition.Position).Length >
                _owner.GetComponent<AiControllerComponent>().VisionRadius)
            {
                // Not necessarily a hard fail, more of a soft fail
                return Outcome.Failed;
            }

            // Unless RangedWeaponComponent is removed from hitscan weapons this shouldn't happen
            if (!equippedWeapon.TryGetComponent(out RangedWeaponComponent rangedWeaponComponent))
            {
                return Outcome.Failed;
            }

            // TODO: Accuracy
            rangedWeaponComponent.AiFire(_owner, _target.Transform.GridPosition);
            return Outcome.Continuing;
        }
    }
}
