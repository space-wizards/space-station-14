using Content.Server.GameObjects;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.GameObjects.Components.Weapon.Melee;
using Content.Server.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Server.AI.Operators.Combat.Melee
{
    public class XenoSwingMeleeWeaponOperator : AiOperator
    {
        private float _burstTime;
        private float _elapsedTime;

        private readonly IEntity _owner;
        private readonly IEntity _target;

        public XenoSwingMeleeWeaponOperator(IEntity owner, IEntity target, float burstTime = 0.0f)
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

            var spriteComponent = _owner.GetComponent<SpriteComponent>();
            spriteComponent.LayerSetState(0, "standing");
            var moveModifer = _owner.GetComponent<MovementSpeedModifierComponent>();
            moveModifer.BaseSprintSpeed *= 0.5f;

            return true;
        }

        public override void Shutdown(Outcome outcome)
        {
            base.Shutdown(outcome);
            if (_owner.TryGetComponent(out CombatModeComponent combatModeComponent) && combatModeComponent.IsInCombatMode)
            {
                combatModeComponent.IsInCombatMode = false;
            }

            var spriteComponent = _owner.GetComponent<SpriteComponent>();
            spriteComponent.LayerSetState(0, "running");
            var moveModifer = _owner.GetComponent<MovementSpeedModifierComponent>();
            moveModifer.BaseSprintSpeed *= 2.0f;
        }

        public override Outcome Execute(float frameTime)
        {
            DebugTools.Assert(_owner.GetComponent<CombatModeComponent>().IsInCombatMode);
            if (_burstTime > 0 && _burstTime <= _elapsedTime)
            {
                return Outcome.Success;
            }

            // Could maaayybbbeee use DamageStates instead but that only seems to be for Species
            if (!_target.TryGetComponent(out DamageableComponent damageableComponent))
            {
                return Outcome.Failed;
            }

            if (damageableComponent.IsDead())
            {
                return Outcome.Success;
            }

            if (!_owner.TryGetComponent(out HandsComponent hands) || hands.GetActiveHand == null)
            {
                return Outcome.Failed;
            }

            var meleeWeapon = hands.GetActiveHand.Owner;
            meleeWeapon.TryGetComponent(out MeleeWeaponComponent meleeWeaponComponent);

            if ((_target.Transform.GridPosition.Position - _owner.Transform.GridPosition.Position).Length >
                meleeWeaponComponent.Range)
            {
                return Outcome.Failed;
            }

            var interactionSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InteractionSystem>();

            interactionSystem.UseItemInHand(_owner, _target.Transform.GridPosition, _target.Uid);
            _elapsedTime += frameTime;
            return Outcome.Continuing;
        }
    }

}
