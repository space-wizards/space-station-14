using Content.Client.Items;
using Content.Client.Weapons.Ranged.UI;
using Content.Shared.Item;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    public ProtoId<ItemStatusPrototype> MagazineItemStatus = "Magazine";

    protected override void InitializeMagazine()
    {
        base.InitializeMagazine();
        SubscribeLocalEvent<MagazineAmmoProviderComponent, UpdateAmmoCounterEvent>(OnMagazineAmmoUpdate);
        SubscribeLocalEvent<MagazineAmmoProviderComponent, AmmoCounterControlEvent>(OnMagazineControl);

        Subs.ItemStatus<BallisticAmmoProviderComponent>(entity => new MagazineStatusControl(entity), MagazineItemStatus);
    }

    private void OnMagazineAmmoUpdate(Entity<MagazineAmmoProviderComponent> ent, ref UpdateAmmoCounterEvent args)
    {
        var magEnt = GetMagazineEntity(ent);

        if (magEnt == null)
        {
            if (args.Control is DefaultStatusControl control)
            {
                control.Update(0, 0);
            }

            return;
        }

        RaiseLocalEvent(magEnt.Value, args, false);
    }

    private void OnMagazineControl(Entity<MagazineAmmoProviderComponent> ent, ref AmmoCounterControlEvent args)
    {
        var magEnt = GetMagazineEntity(ent);
        if (magEnt == null)
            return;
        RaiseLocalEvent(magEnt.Value, args, false);
    }
}
