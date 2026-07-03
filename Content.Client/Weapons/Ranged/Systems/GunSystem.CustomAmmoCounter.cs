using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void InitializeCustomAmmoCounter()
    {
        base.InitializeCustomAmmoCounter();

        SubscribeLocalEvent<CustomSpriteAmmoCounterComponent, UpdateAmmoCounterEvent>(OnAmmoCountUpdate);
        SubscribeLocalEvent<CustomSpriteAmmoCounterComponent, AmmoCounterControlEvent>(OnControl);
    }

    private void OnAmmoCountUpdate(Entity<CustomSpriteAmmoCounterComponent> ent, ref UpdateAmmoCounterEvent args)
    {
        if (args.Control is not CustomIconStatusControl customIcon)
            return;

        var ammoEv = new GetAmmoCountEvent();
        RaiseLocalEvent(ent, ref ammoEv);

        if (HasComp<ChamberMagazineAmmoProviderComponent>(ent))
        {
            var chambered = GetChamberEntity(ent);
            var magEntity = GetMagazineEntity(ent);
            customIcon.Update(
                ammoEv.Count,
                ammoEv.Capacity,
                magEntity != null,
                true,
                chambered != null);
            return;
        }

        customIcon.Update(ammoEv.Count, ammoEv.Capacity);
    }

    private void OnControl(Entity<CustomSpriteAmmoCounterComponent> ent, ref AmmoCounterControlEvent args)
    {
        var loadedTexture = _sprite.GetFrame(ent.Comp.LoadedAmmoSprite, TimeSpan.Zero);
        var spentTexture = _sprite.GetFrame(ent.Comp.SpentAmmoSprite, TimeSpan.Zero);

        args.Control = new CustomIconStatusControl(loadedTexture, spentTexture, ent.Comp.NumberOfRows);
    }
}
