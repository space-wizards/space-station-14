using System;
using Content.Server.GameObjects.Components.Weapon.Ranged.Hitscan;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Operators.Combat.Ranged
{
    public class WaitForHitscanChargeOperator : AiOperator
    {
        private float _lastCharge = 0.0f;
        private float _lastFill = 0.0f;
        private HitscanWeaponComponent _hitscan;

        public WaitForHitscanChargeOperator(IEntity entity)
        {
            if (!entity.TryGetComponent(out HitscanWeaponComponent hitscanWeaponComponent))
            {
                throw new InvalidOperationException();
            }

            _hitscan = hitscanWeaponComponent;
        }

        public override Outcome Execute(float frameTime)
        {
            if (_hitscan.CapacitorComponent.Capacity - _hitscan.CapacitorComponent.Charge < 0.01f)
            {
                return Outcome.Success;
            }

            // If we're not charging then just stop
            _lastFill = _hitscan.CapacitorComponent.Charge - _lastCharge;
            _lastCharge = _hitscan.CapacitorComponent.Charge;

            if (_lastFill == 0.0f)
            {
                return Outcome.Failed;
            }
            return Outcome.Continuing;
        }
    }
}
