using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    protected virtual void InitializeBasicEntity()
    {
        SubscribeLocalEvent<BasicEntityAmmoProviderComponent, MapInitEvent>(OnBasicEntityMapInit);
        SubscribeLocalEvent<BasicEntityAmmoProviderComponent, TakeAmmoEvent>(OnBasicEntityTakeAmmo);
        SubscribeLocalEvent<BasicEntityAmmoProviderComponent, GetAmmoCountEvent>(OnBasicEntityAmmoCount);
    }

    private void OnBasicEntityMapInit(Entity<BasicEntityAmmoProviderComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Count is null)
        {
            ent.Comp.Count = ent.Comp.Capacity;
            Dirty(ent);
        }

        UpdateBasicEntityAppearance(ent);
    }

    private void OnBasicEntityTakeAmmo(Entity<BasicEntityAmmoProviderComponent> ent, ref TakeAmmoEvent args)
    {
        for (var i = 0; i < args.Shots; i++)
        {
            if (ent.Comp.Count <= 0)
                return;

            if (ent.Comp.Count != null)
                ent.Comp.Count--;

            var ammoEnt = Spawn(ent.Comp.Proto, args.Coordinates);
            args.Ammo.Add((ammoEnt, EnsureShootable(ammoEnt)));
        }

        _recharge.Reset(ent.Owner);
        UpdateBasicEntityAppearance(ent);
        Dirty(ent);
    }

    private void OnBasicEntityAmmoCount(Entity<BasicEntityAmmoProviderComponent> ent, ref GetAmmoCountEvent args)
    {
        args.Capacity = ent.Comp.Capacity ?? int.MaxValue;
        args.Count = ent.Comp.Count ?? int.MaxValue;
    }

    private void UpdateBasicEntityAppearance(Entity<BasicEntityAmmoProviderComponent> ent)
    {
        if (!Timing.IsFirstTimePredicted || !TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        Appearance.SetData(ent, AmmoVisuals.HasAmmo, ent.Comp.Count != 0, appearance);
        Appearance.SetData(ent, AmmoVisuals.AmmoCount, ent.Comp.Count ?? int.MaxValue, appearance);
        Appearance.SetData(ent, AmmoVisuals.AmmoMax, ent.Comp.Capacity ?? int.MaxValue, appearance);
    }

    #region Public API
    public bool ChangeBasicEntityAmmoCount(Entity<BasicEntityAmmoProviderComponent?> ent, int delta)
    {
        if (!Resolve(ent, ref ent.Comp, false) || ent.Comp.Count == null)
            return false;

        return UpdateBasicEntityAmmoCount((ent.Owner, ent.Comp), ent.Comp.Count.Value + delta);
    }

    public bool UpdateBasicEntityAmmoCount(Entity<BasicEntityAmmoProviderComponent?> ent, int count)
    {
        if (!Resolve(ent, ref ent.Comp, false) || count > ent.Comp.Capacity)
            return false;

        ent.Comp.Count = count;
        UpdateBasicEntityAppearance((ent.Owner, ent.Comp));
        UpdateAmmoCount(ent);
        Dirty(ent);

        return true;
    }

    #endregion
}
