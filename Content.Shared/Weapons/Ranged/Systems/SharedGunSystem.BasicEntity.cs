using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    protected virtual void InitializeBasicEntity()
    {
        SubscribeLocalEvent<BasicEntityAmmoProviderComponent, MapInitEvent>(OnBasicEntityMapInit);
        SubscribeLocalEvent<BasicEntityAmmoProviderComponent, TakeAmmoEvent>(OnBasicEntityTakeAmmo);
        SubscribeLocalEvent<BasicEntityAmmoProviderComponent, GetAmmoCountEvent>(OnBasicEntityAmmoCount);
    }

    private void OnBasicEntityMapInit(EntityUid uid, BasicEntityAmmoProviderComponent component, MapInitEvent args)
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
        for (var i = 0; i < args.Shots; i++)
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

        _recharge.Reset(uid);
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
        UpdateAmmoCount(uid);

        return true;
    }

    #endregion
}
