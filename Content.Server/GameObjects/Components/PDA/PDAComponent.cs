using Content.Server.GameObjects.Components.Access;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.PDA;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;

namespace Content.Server.PDA
{
    [RegisterComponent]
    public class PDAComponent : SharedPDAComponent, IAttackBy
    {
        private Container _idSlot;

        public bool AttackBy(AttackByEventArgs eventArgs)
        {
            var item = eventArgs.AttackWith;
            if (_idSlot.ContainedEntities.Count > 0)
            {
                return false;
            }

            if (item.TryGetComponent<IdCardComponent>(out var idCardComponent) && !_idSlot.Contains(item))
            {
                _idSlot.Insert(item);
            }

            return false;
        }
    }
}
