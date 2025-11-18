using Content.Client.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void InitializeChamberMagazine()
    {
        base.InitializeChamberMagazine();
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, AmmoCounterControlEvent>(OnChamberMagazineCounter);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, UpdateAmmoCounterEvent>(OnChamberMagazineAmmoUpdate);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, AppearanceChangeEvent>(OnChamberMagazineAppearance);
    }

    private void OnChamberMagazineAppearance(EntityUid uid, ChamberMagazineAmmoProviderComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null ||
            !_sprite.LayerMapTryGet((uid, args.Sprite), GunVisualLayers.Base, out var boltLayer, false) ||
            !Appearance.TryGetData(uid, AmmoVisuals.BoltClosed, out bool boltClosed))
        {
            return;
        }

        // Maybe re-using base layer for this will bite me someday but screw you future sloth.
        if (boltClosed)
        {
            _sprite.LayerSetRsiState((uid, args.Sprite), boltLayer, "base");
        }
        else
        {
            _sprite.LayerSetRsiState((uid, args.Sprite), boltLayer, "bolt-open");
        }
    }

    protected override void OnMagazineSlotChange(EntityUid uid, MagazineAmmoProviderComponent component, ContainerModifiedMessage args)
    {
        base.OnMagazineSlotChange(uid, component, args);

        if (ChamberSlot != args.Container.ID || args is not EntRemovedFromContainerMessage removedArgs)
            return;

        // This is dirty af. Prediction moment.
        // We may be predicting spawning entities and the engine just removes them from the container so we'll just delete them.
        if (IsClientSide(removedArgs.Entity))
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
