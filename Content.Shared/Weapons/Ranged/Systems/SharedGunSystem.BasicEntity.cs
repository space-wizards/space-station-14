using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    protected virtual void InitializeBasicEntity()
    {
        SubscribeLocalEvent<BasicEntityAmmoProviderComponent, ComponentInit>(OnBasicEntityInit);
        SubscribeLocalEvent<BasicEntityAmmoProviderComponent, TakeAmmoEvent>(OnBasicEntityTakeAmmo);
        SubscribeLocalEvent<BasicEntityAmmoProviderComponent, GetAmmoCountEvent>(OnBasicEntityAmmoCount);

        SubscribeLocalEvent<BasicEntityAmmoProviderComponent, ComponentGetState>(OnBasicEntityGetState);
        SubscribeLocalEvent<BasicEntityAmmoProviderComponent, ComponentHandleState>(OnBasicEntityHandleState);
    }

    private void OnBasicEntityGetState(EntityUid uid, BasicEntityAmmoProviderComponent component, ref ComponentGetState args)
    {
        args.State = new BasicEntityAmmoProviderComponentState(component.Capacity, component.Count);
    }

    private void OnBasicEntityHandleState(EntityUid uid, BasicEntityAmmoProviderComponent component, ref ComponentHandleState args)
    {
        if (args.Current is BasicEntityAmmoProviderComponentState state)
        {
            component.Capacity = state.Capacity;
            component.Count = state.Count;
        }
    }

    private void OnBasicEntityInit(EntityUid uid, BasicEntityAmmoProviderComponent component, ComponentInit args)
    {
        if (component.Count is null)
        {
            component.Count = component.Capacity;
            Dirty(component);
        }
    }

    private void OnBasicEntityTakeAmmo(EntityUid uid, BasicEntityAmmoProviderComponent component, TakeAmmoEvent args)
    {
        if (component.Count < 0)
            return;

        for (int i = 0; i < args.Shots; i++)
        {
            if (component.Count != null)
            {
                component.Count--;
            }

            var ent = Spawn(component.Proto, args.Coordinates);
            args.Ammo.Add(EnsureComp<AmmoComponent>(ent));
        }

        Dirty(component);
    }

    private void OnBasicEntityAmmoCount(EntityUid uid, BasicEntityAmmoProviderComponent component, ref GetAmmoCountEvent args)
    {
        args.Capacity = component.Capacity ?? Int32.MaxValue;
        args.Count = component.Count ?? Int32.MaxValue;
    }
}
