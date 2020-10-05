using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Ammunition
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedAmmoComponent))]
    public sealed class AmmoComponent : SharedAmmoComponent
    {
        public override bool Spent
        {
            get => _spent;
            set
            {
                if (_spent == value)
                {
                    return;
                }

                _spent = value;

                if (_spent)
                {
                    if (Caseless)
                    {
                        Owner.Delete();
                        return;
                    }
                }
                
                Dirty();
            }
        }
        private bool _spent;

        public override ComponentState GetComponentState()
        {
            return new AmmoComponentState(Spent);
        }
    }
}
