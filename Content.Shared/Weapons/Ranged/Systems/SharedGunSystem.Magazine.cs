using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    protected const string MagazineSlot = "gun_magazine";

    protected virtual void InitializeMagazine()
    {
        SubscribeLocalEvent<MagazineAmmoProviderComponent, TakeAmmoEvent>(OnMagazineTakeAmmo);
        SubscribeLocalEvent<MagazineAmmoProviderComponent, GetVerbsEvent<Verb>>(OnMagazineVerb);
        SubscribeLocalEvent<MagazineAmmoProviderComponent, ItemSlotChangedEvent>(OnMagazineSlotChange);
        SubscribeLocalEvent<MagazineAmmoProviderComponent, UseInHandEvent>(OnMagazineUse);
        SubscribeLocalEvent<MagazineAmmoProviderComponent, ExaminedEvent>(OnMagazineExamine);
    }

    private void OnMagazineExamine(EntityUid uid, MagazineAmmoProviderComponent component, ExaminedEvent args)
    {
        var (count, _) = GetMagazineCountCapacity(component);
        args.PushMarkup(Loc.GetString("gun-magazine-examine", ("color", AmmoExamineColor), ("count", count)));
    }

    private void OnMagazineUse(EntityUid uid, MagazineAmmoProviderComponent component, UseInHandEvent args)
    {
        var magEnt = GetMagazineEntity(uid);

        if (magEnt == null) return;

        RaiseLocalEvent(magEnt.Value, args, false);
        UpdateAmmoCount(uid);
        UpdateMagazineAppearance(component, magEnt.Value);
    }

    private void OnMagazineVerb(EntityUid uid, MagazineAmmoProviderComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !args.CanAccess) return;

        var magEnt = GetMagazineEntity(uid);

        if (magEnt != null)
        {
            RaiseLocalEvent(magEnt.Value, args, false);
            UpdateMagazineAppearance(component, magEnt.Value);
        }
    }

    private void OnMagazineSlotChange(EntityUid uid, MagazineAmmoProviderComponent component, ref ItemSlotChangedEvent args)
    {
        UpdateAmmoCount(uid);
        if (!TryComp<AppearanceComponent>(uid, out var appearance)) return;
        appearance.SetData(AmmoVisuals.MagLoaded, GetMagazineEntity(uid) != null);
    }

    protected (int, int) GetMagazineCountCapacity(MagazineAmmoProviderComponent component)
    {
        var count = 0;
        var capacity = 1;
        var magEnt = GetMagazineEntity(component.Owner);

        if (magEnt != null)
        {
            var ev = new GetAmmoCountEvent();
            RaiseLocalEvent(magEnt.Value, ref ev, false);
            count += ev.Count;
            capacity += ev.Capacity;
        }

        return (count, capacity);
    }

    protected EntityUid? GetMagazineEntity(EntityUid uid)
    {
        if (!Containers.TryGetContainer(uid, MagazineSlot, out var container) ||
            container is not ContainerSlot slot) return null;
        return slot.ContainedEntity;
    }

    private void OnMagazineTakeAmmo(EntityUid uid, MagazineAmmoProviderComponent component, TakeAmmoEvent args)
    {
        var magEntity = GetMagazineEntity(uid);
        TryComp<AppearanceComponent>(uid, out var appearance);

        if (magEntity == null)
        {
            appearance?.SetData(AmmoVisuals.MagLoaded, false);
            return;
        }

        // Pass the event onwards.
        RaiseLocalEvent(magEntity.Value, args, false);
        // Should be Dirtied by what other ammoprovider is handling it.

        var ammoEv = new GetAmmoCountEvent();
        RaiseLocalEvent(magEntity.Value, ref ammoEv, false);
        FinaliseMagazineTakeAmmo(uid, component, args, ammoEv.Count, ammoEv.Capacity, appearance);
    }

    private void FinaliseMagazineTakeAmmo(EntityUid uid, MagazineAmmoProviderComponent component, TakeAmmoEvent args, int count, int capacity, AppearanceComponent? appearance)
    {
        // If no ammo then check for autoeject
        if (component.AutoEject && args.Ammo.Count == 0)
        {
            EjectMagazine(component);
            PlaySound(uid, component.SoundAutoEject?.GetSound(Random, ProtoManager), args.User);
        }

        UpdateMagazineAppearance(appearance, true, count, capacity);
    }

    private void UpdateMagazineAppearance(MagazineAmmoProviderComponent component, EntityUid magEnt)
    {
        TryComp<AppearanceComponent>(component.Owner, out var appearance);

        var count = 0;
        var capacity = 0;

        if (component is ChamberMagazineAmmoProviderComponent chamber)
        {
            count = GetChamberEntity(chamber.Owner) != null ? 1 : 0;
            capacity = 1;
        }

        if (TryComp<AppearanceComponent>(magEnt, out var magAppearance))
        {
            magAppearance.TryGetData<int>(AmmoVisuals.AmmoCount, out var addCount);
            magAppearance.TryGetData<int>(AmmoVisuals.AmmoMax, out var addCapacity);
            count += addCount;
            capacity += addCapacity;
        }

        UpdateMagazineAppearance(appearance, true, count, capacity);
    }

    private void UpdateMagazineAppearance(AppearanceComponent? appearance, bool magLoaded, int count, int capacity)
    {
        // Copy the magazine's appearance data
        appearance?.SetData(AmmoVisuals.MagLoaded, magLoaded);
        appearance?.SetData(AmmoVisuals.HasAmmo, count != 0);
        appearance?.SetData(AmmoVisuals.AmmoCount, count);
        appearance?.SetData(AmmoVisuals.AmmoMax, capacity);
    }

    private void EjectMagazine(MagazineAmmoProviderComponent component)
    {
        var ent = GetMagazineEntity(component.Owner);

        if (ent == null) return;

        _slots.TryEject(component.Owner, MagazineSlot, null, out var a, excludeUserAudio: true);
    }
}
