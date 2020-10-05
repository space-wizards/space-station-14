#nullable enable
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Weapons.Ranged
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedAmmoComponent))]
    internal sealed class AmmoComponent : SharedAmmoComponent
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
                
                if (Owner.TryGetComponent(out AppearanceComponent? appearanceComponent))
                {
                    appearanceComponent.SetData(AmmoVisuals.Spent, value);
                }
            }
        }
        
        private bool _spent;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (!(curState is AmmoComponentState state))
                return;

            Spent = state.Spent;
        }
    }
}