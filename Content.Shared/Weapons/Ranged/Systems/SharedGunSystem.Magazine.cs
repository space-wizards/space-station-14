using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    protected const string MagazineSlot = "gun_magazine";

    protected virtual void InitializeMagazine()
    {
        SubscribeLocalEvent<MagazineAmmoProviderComponent, MapInitEvent>(OnMagazineMapInit);
        SubscribeLocalEvent<MagazineAmmoProviderComponent, TakeAmmoEvent>(OnMagazineTakeAmmo);
        SubscribeLocalEvent<MagazineAmmoProviderComponent, GetAmmoCountEvent>(OnMagazineAmmoCount);
        SubscribeLocalEvent<MagazineAmmoProviderComponent, GetVerbsEvent<AlternativeVerb>>(OnMagazineVerb);
        SubscribeLocalEvent<MagazineAmmoProviderComponent, EntInsertedIntoContainerMessage>(OnMagazineSlotChange);
        SubscribeLocalEvent<MagazineAmmoProviderComponent, EntRemovedFromContainerMessage>(OnMagazineSlotChange);
        SubscribeLocalEvent<MagazineAmmoProviderComponent, UseInHandEvent>(OnMagazineUse);
        SubscribeLocalEvent<MagazineAmmoProviderComponent, ExaminedEvent>(OnMagazineExamine);
    }

    private void OnMagazineMapInit(Entity<MagazineAmmoProviderComponent> ent, ref MapInitEvent args)
    {
        MagazineSlotChanged(ent);
    }

    private void OnMagazineExamine(EntityUid uid, MagazineAmmoProviderComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var (count, _) = GetMagazineCountCapacity(uid, component);
        args.PushMarkup(Loc.GetString("gun-magazine-examine", ("color", AmmoExamineColor), ("count", count)));
    }

    private void OnMagazineUse(EntityUid uid, MagazineAmmoProviderComponent component, UseInHandEvent args)
    {
        var magEnt = GetMagazineEntity(uid);

        if (magEnt == null)
            return;

        RaiseLocalEvent(magEnt.Value, args);
        UpdateAmmoCount(uid);
        UpdateMagazineAppearance(uid, component, magEnt.Value);
    }

    private void OnMagazineVerb(EntityUid uid, MagazineAmmoProviderComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var magEnt = GetMagazineEntity(uid);

        if (magEnt != null)
        {
            RaiseLocalEvent(magEnt.Value, args);
            UpdateMagazineAppearance(magEnt.Value, component, magEnt.Value);
        }
    }

    protected virtual void OnMagazineSlotChange(EntityUid uid, MagazineAmmoProviderComponent component, ContainerModifiedMessage args)
    {
        if (MagazineSlot != args.Container.ID)
            return;

        MagazineSlotChanged((uid, component));
    }

    private void MagazineSlotChanged(Entity<MagazineAmmoProviderComponent> ent)
    {
        UpdateAmmoCount(ent);
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        var magEnt = GetMagazineEntity(ent);
        Appearance.SetData(ent, AmmoVisuals.MagLoaded, magEnt != null, appearance);

        if (magEnt != null)
        {
            UpdateMagazineAppearance(ent, ent, magEnt.Value);
        }
    }

    protected (int, int) GetMagazineCountCapacity(EntityUid uid, MagazineAmmoProviderComponent component)
    {
        var count = 0;
        var capacity = 1;
        var magEnt = GetMagazineEntity(uid);

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
            container is not ContainerSlot slot)
        {
            return null;
        }

        return slot.ContainedEntity;
    }

    private void OnMagazineAmmoCount(EntityUid uid, MagazineAmmoProviderComponent component, ref GetAmmoCountEvent args)
    {
        var magEntity = GetMagazineEntity(uid);

        if (magEntity == null)
            return;

        RaiseLocalEvent(magEntity.Value, ref args);
    }

    private void OnMagazineTakeAmmo(EntityUid uid, MagazineAmmoProviderComponent component, TakeAmmoEvent args)
    {
        var magEntity = GetMagazineEntity(uid);
        TryComp<AppearanceComponent>(uid, out var appearance);

        if (magEntity == null)
        {
            Appearance.SetData(uid, AmmoVisuals.MagLoaded, false, appearance);
            return;
        }

        // Pass the event onwards.
        RaiseLocalEvent(magEntity.Value, args);
        // Should be Dirtied by what other ammoprovider is handling it.

        var ammoEv = new GetAmmoCountEvent();
        RaiseLocalEvent(magEntity.Value, ref ammoEv);
        FinaliseMagazineTakeAmmo(uid, component, ammoEv.Count, ammoEv.Capacity, args.User, appearance);
    }

    private void FinaliseMagazineTakeAmmo(EntityUid uid, MagazineAmmoProviderComponent component, int count, int capacity, EntityUid? user, AppearanceComponent? appearance)
    {
        // If no ammo then check for autoeject
        var ejectMag = component.AutoEject && count == 0;
        if (ejectMag)
        {
            EjectMagazine(uid, component);
            Audio.PlayPredicted(component.SoundAutoEject, uid, user);
        }

        UpdateMagazineAppearance(uid, appearance, !ejectMag, count, capacity);
    }

    private void UpdateMagazineAppearance(EntityUid uid, MagazineAmmoProviderComponent component, EntityUid magEnt)
    {
        TryComp<AppearanceComponent>(uid, out var appearance);

        var count = 0;
        var capacity = 0;

        if (TryComp<AppearanceComponent>(magEnt, out var magAppearance))
        {
            Appearance.TryGetData<int>(magEnt, AmmoVisuals.AmmoCount, out var addCount, magAppearance);
            Appearance.TryGetData<int>(magEnt, AmmoVisuals.AmmoMax, out var addCapacity, magAppearance);
            count += addCount;
            capacity += addCapacity;
        }

        UpdateMagazineAppearance(uid, appearance, true, count, capacity);
    }

    private void UpdateMagazineAppearance(EntityUid uid, AppearanceComponent? appearance, bool magLoaded, int count, int capacity)
    {
        if (appearance == null)
            return;

        // Copy the magazine's appearance data
        Appearance.SetData(uid, AmmoVisuals.MagLoaded, magLoaded, appearance);
        Appearance.SetData(uid, AmmoVisuals.HasAmmo, count != 0, appearance);
        Appearance.SetData(uid, AmmoVisuals.AmmoCount, count, appearance);
        Appearance.SetData(uid, AmmoVisuals.AmmoMax, capacity, appearance);
    }

    private void EjectMagazine(EntityUid uid, MagazineAmmoProviderComponent component)
    {
        var ent = GetMagazineEntity(uid);

        if (ent == null)
            return;

        _slots.TryEject(uid, MagazineSlot, null, out var a, excludeUserAudio: true);
    }
}
