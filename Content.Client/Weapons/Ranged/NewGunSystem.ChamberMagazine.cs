using Content.Shared.Containers.ItemSlots;
using Content.Shared.Weapons.Ranged;

namespace Content.Client.Weapons.Ranged;

public sealed partial class NewGunSystem
{
    protected override void InitializeChamberMagazine()
    {
        base.InitializeChamberMagazine();
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, AmmoCounterControlEvent>(OnChamberMagazineCounter);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, UpdateAmmoCounterEvent>(OnChamberMagazineAmmoUpdate);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, ItemSlotChangedEvent>(OnChamberMagazineSlotChange);
    }

    private void OnChamberMagazineSlotChange(EntityUid uid, ChamberMagazineAmmoProviderComponent component, ItemSlotChangedEvent args)
    {
        UpdateAmmoCount(uid);
    }

    private void OnChamberMagazineCounter(EntityUid uid, ChamberMagazineAmmoProviderComponent component, AmmoCounterControlEvent args)
    {
        args.Control = new ChamberMagazineStatusControl();
    }

    private void OnChamberMagazineAmmoUpdate(EntityUid uid, ChamberMagazineAmmoProviderComponent component, UpdateAmmoCounterEvent args)
    {
        if (args.Control is not ChamberMagazineStatusControl control) return;

        var chambered = GetChamberEntity(uid);
        var magEntity = GetMagazineEntity(uid);
        var ammoCountEv = new GetAmmoCountEvent();

        if (magEntity != null)
            RaiseLocalEvent(magEntity.Value, ref ammoCountEv);

        control.Update(chambered != null, magEntity != null, ammoCountEv.Count, ammoCountEv.Capacity);
    }
}
