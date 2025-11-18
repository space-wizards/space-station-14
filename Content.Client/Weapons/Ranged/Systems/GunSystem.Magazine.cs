using Content.Shared.Weapons.Ranged;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void InitializeMagazine()
    {
        base.InitializeMagazine();
        SubscribeLocalEvent<MagazineAmmoProviderComponent, UpdateAmmoCounterEvent>(OnMagazineAmmoUpdate);
        SubscribeLocalEvent<MagazineAmmoProviderComponent, AmmoCounterControlEvent>(OnMagazineControl);
    }

    private void OnMagazineAmmoUpdate(EntityUid uid, MagazineAmmoProviderComponent component, UpdateAmmoCounterEvent args)
    {
        var ent = GetMagazineEntity(uid);

        if (ent == null)
        {
            if (args.Control is DefaultStatusControl control)
            {
                control.Update(0, 0);
            }

            return;
        }

        RaiseLocalEvent(ent.Value, args, false);
    }

    private void OnMagazineControl(EntityUid uid, MagazineAmmoProviderComponent component, AmmoCounterControlEvent args)
    {
        var ent = GetMagazineEntity(uid);
        if (ent == null)
            return;
        RaiseLocalEvent(ent.Value, args, false);
    }
}
