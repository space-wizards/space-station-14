using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    protected virtual void InitializeCartridge()
    {
        SubscribeLocalEvent<CartridgeAmmoComponent, ComponentGetState>(OnCartridgeGetState);
        SubscribeLocalEvent<CartridgeAmmoComponent, ComponentHandleState>(OnCartridgeHandleState);
    }

    private void OnCartridgeHandleState(EntityUid uid, CartridgeAmmoComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not CartridgeAmmoComponentState state) return;
        component.Spent = state.Spent;
    }

    private void OnCartridgeGetState(EntityUid uid, CartridgeAmmoComponent component, ref ComponentGetState args)
    {
        args.State = new CartridgeAmmoComponentState()
        {
            Spent = component.Spent,
        };
    }

    [Serializable, NetSerializable]
    private sealed class CartridgeAmmoComponentState : ComponentState
    {
        public bool Spent;
    }
}
