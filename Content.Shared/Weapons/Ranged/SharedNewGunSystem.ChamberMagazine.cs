using Robust.Shared.Containers;

namespace Content.Shared.Weapons.Ranged;

public abstract partial class SharedNewGunSystem
{
    // Chamber + mags in one package.
    private void InitializeChamberMagazine()
    {
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, ComponentInit>(OnChamberMagazineInit);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, TakeAmmoEvent>(OnChamberMagazineTakeAmmo);
    }

    private void OnChamberMagazineInit(EntityUid uid, ChamberMagazineAmmoProviderComponent component, ComponentInit args)
    {
        component.Chamber = Containers.EnsureContainer<ContainerSlot>(uid, "chamber-ammo");
        component.Magazine = Containers.EnsureContainer<ContainerSlot>(uid, "magazine-ammo");
    }

    private void OnChamberMagazineTakeAmmo(EntityUid uid, ChamberMagazineAmmoProviderComponent component, TakeAmmoEvent args)
    {
        for (var i = 0; i < args.Shots; i++)
        {
            var ent = component.Chamber.ContainedEntity;
            if (ent != null)
            {
                args.Ammo.Add(EnsureComp<NewAmmoComponent>(ent.Value));
                component.Chamber.Remove(ent.Value);
            }
            else
            {
                args.Shots -= 1;
            }

            // Pass an event on to re-fill the chamber
            if (component.Magazine.ContainedEntity != null)
            {
                var ev = new TakeAmmoEvent
                {
                    Ammo = new List<IShootable>(),
                    Coordinates = args.Coordinates,
                    Shots = 1,
                };

                RaiseLocalEvent(component.Magazine.ContainedEntity.Value, ev);

                if (ev.Shots == 1)
                {
                    component.Chamber.Insert(((NewAmmoComponent) ev.Ammo[0]).Owner);
                }
            }
        }
    }
}
