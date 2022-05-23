using Robust.Shared.Containers;

namespace Content.Shared.Weapons.Ranged;

public abstract partial class SharedNewGunSystem
{
    private void InitializeMagazine()
    {
        SubscribeLocalEvent<MagazineAmmoProviderComponent, ComponentInit>(OnMagazineInit);
        SubscribeLocalEvent<MagazineAmmoProviderComponent, TakeAmmoEvent>(OnMagazineTakeAmmo);
    }

    private void OnMagazineInit(EntityUid uid, MagazineAmmoProviderComponent component, ComponentInit args)
    {
        component.Magazine = Containers.EnsureContainer<ContainerSlot>(uid, "magazine-ammo");
    }

    private void OnMagazineTakeAmmo(EntityUid uid, MagazineAmmoProviderComponent component, TakeAmmoEvent args)
    {
        var ent = component.Magazine.ContainedEntity;

        if (ent == null)
        {
            args.Shots = 0;
            return;
        }

        // Pass the event onwards.
        RaiseLocalEvent(ent.Value, args);
    }
}
