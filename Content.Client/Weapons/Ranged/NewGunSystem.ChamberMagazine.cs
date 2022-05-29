using Content.Shared.Weapons.Ranged;

namespace Content.Client.Weapons.Ranged;

public sealed partial class NewGunSystem
{
    protected override void InitializeChamberMagazine()
    {
        base.InitializeChamberMagazine();
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, UpdateAmmoCounterEvent>(OnChamberMagazineAmmoUpdate);
    }

    private void OnChamberMagazineControl(EntityUid uid, ChamberMagazineAmmoProviderComponent component, AmmoCounterControlEvent args)
    {
        args.Control = new ChamberMagazineStatusControl();
    }

    private void OnChamberMagazineAmmoUpdate(EntityUid uid, MagazineAmmoProviderComponent component, UpdateAmmoCounterEvent args)
    {
        // TODO: Chamber and all this
        var ent = GetMagazineEntity(uid);

        if (ent == null)
        {
            if (args.Control is DefaultStatusControl control)
            {
                control.Update(0, 0);
            }

            return;
        }

        RaiseLocalEvent(ent.Value, args);
    }
}
