using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Shared.Weapons.Ranged;

public abstract partial class SharedNewGunSystem
{
    private void InitializeMagazine()
    {
        SubscribeLocalEvent<MagazineAmmoProviderComponent, ComponentInit>(OnMagazineInit);
        SubscribeLocalEvent<MagazineAmmoProviderComponent, TakeAmmoEvent>(OnMagazineTakeAmmo);
        SubscribeLocalEvent<MagazineAmmoProviderComponent, GetVerbsEvent<Verb>>(OnMagazineVerb);
    }

    private void OnMagazineVerb(EntityUid uid, MagazineAmmoProviderComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !args.CanAccess) return;

        var magEnt = component.Magazine.ContainedEntity;

        var verb = new Verb()
        {
            Text = "Eject magazine",
            Disabled = magEnt == null,
            Act = () => EjectMagazine(component)
        };

        args.Verbs.Add(verb);

        if (magEnt != null)
            RaiseLocalEvent(magEnt.Value, args);
    }

    private void OnMagazineInit(EntityUid uid, MagazineAmmoProviderComponent component, ComponentInit args)
    {
        component.Magazine = Containers.EnsureContainer<ContainerSlot>(uid, "magazine-ammo");
    }

    private void OnMagazineTakeAmmo(EntityUid uid, MagazineAmmoProviderComponent component, TakeAmmoEvent args)
    {
        var ent = component.Magazine.ContainedEntity;

        if (ent == null) return;

        // Pass the event onwards.
        RaiseLocalEvent(ent.Value, args);
        // Should be Dirtied by what other ammoprovider is handling it.

        // If no ammo then check for autoeject
        if (component.AutoEject && args.Ammo.Count == 0)
        {
            EjectMagazine(component);
            var sound = component.SoundAutoEject?.GetSound();

            if (sound != null)
                SoundSystem.Play(Filter.Pvs(uid, entityManager: EntityManager), sound);
        }

    }

    private void EjectMagazine(MagazineAmmoProviderComponent component)
    {
        var ent = component.Magazine.ContainedEntity;

        if (ent == null) return;

        component.Magazine.Remove(ent.Value);

        if (component.SoundMagEject != null)
            SoundSystem.Play(Filter.Pvs(component.Owner, entityManager: EntityManager),
                component.SoundMagEject.GetSound());
    }
}
