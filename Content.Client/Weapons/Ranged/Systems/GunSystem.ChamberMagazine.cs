using Content.Shared.Examine;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void InitializeChamberMagazine()
    {
        base.InitializeChamberMagazine();
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, AmmoCounterControlEvent>(OnChamberMagazineCounter);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, UpdateAmmoCounterEvent>(OnChamberMagazineAmmoUpdate);
    }

    protected override void OnMagazineSlotChange(EntityUid uid, MagazineAmmoProviderComponent component, ContainerModifiedMessage args)
    {
        base.OnMagazineSlotChange(uid, component, args);

        if (ChamberSlot != args.Container.ID || args is not EntRemovedFromContainerMessage removedArgs)
            return;

        // This is dirty af. Prediction moment.
        // We may be predicting spawning entities and the engine just removes them from the container so we'll just delete them.
        if (removedArgs.Entity.IsClientSide())
            QueueDel(args.Entity);

        // AFAIK the only main alternative is having some client-specific handling via a bool or otherwise for the state.
        // which is much larger and I'm not sure how much better it is. It's bad enough we have to do it with revolvers
        // to avoid 6-7 additional entity spawns.
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
            RaiseLocalEvent(magEntity.Value, ref ammoCountEv, false);

        control.Update(chambered != null, magEntity != null, ammoCountEv.Count, ammoCountEv.Capacity);
    }
}
