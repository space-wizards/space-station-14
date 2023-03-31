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

        UpdateBasicEntityAppearance(uid, component);
    }

    private void OnBasicEntityTakeAmmo(EntityUid uid, BasicEntityAmmoProviderComponent component, TakeAmmoEvent args)
    {
        for (int i = 0; i < args.Shots; i++)
        {
            if (component.Count <= 0)
                return;

            if (component.Count != null)
            {
                component.Count--;
            }

            var ent = Spawn(component.Proto, args.Coordinates);
            args.Ammo.Add((ent, EnsureComp<AmmoComponent>(ent)));
        }

        UpdateBasicEntityAppearance(uid, component);
        Dirty(component);
    }

    private void OnBasicEntityAmmoCount(EntityUid uid, BasicEntityAmmoProviderComponent component, ref GetAmmoCountEvent args)
    {
        args.Capacity = component.Capacity ?? int.MaxValue;
        args.Count = component.Count ?? int.MaxValue;
    }

    private void UpdateBasicEntityAppearance(EntityUid uid, BasicEntityAmmoProviderComponent component)
    {
        if (!Timing.IsFirstTimePredicted || !TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        Appearance.SetData(uid, AmmoVisuals.HasAmmo, component.Count != 0, appearance);
        Appearance.SetData(uid, AmmoVisuals.AmmoCount, component.Count ?? int.MaxValue, appearance);
        Appearance.SetData(uid, AmmoVisuals.AmmoMax, component.Capacity ?? int.MaxValue, appearance);
    }

    #region Public API

    public bool UpdateBasicEntityAmmoCount(EntityUid uid, int count, BasicEntityAmmoProviderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (count > component.Capacity)
            return false;

        component.Count = count;
        Dirty(component);
        UpdateBasicEntityAppearance(uid, component);

        return true;
    }

    #endregion
}
