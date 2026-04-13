using Content.Shared.Weapons.Ranged.Components;

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

        if (TryComp<BallisticAmmoProviderComponent>(ent, out var ballisticAmmoProviderComp))
        {
            customIcon.Update(ballisticAmmoProviderComp.Count, ballisticAmmoProviderComp.Capacity);
        }
        else if (TryComp<BatteryAmmoProviderComponent>(ent, out var batteryAmmoProviderComp))
        {
            customIcon.Update(batteryAmmoProviderComp.Shots, batteryAmmoProviderComp.Capacity);
        }
        else
            customIcon.Update(0, 0);
    }

    private void OnControl(Entity<CustomSpriteAmmoCounterComponent> ent, ref AmmoCounterControlEvent args)
    {
        var loadedTexture = _sprite.GetFrame(ent.Comp.LoadedAmmoSprite, TimeSpan.Zero);
        var spentTexture = _sprite.GetFrame(ent.Comp.SpentAmmoSprite, TimeSpan.Zero);

        args.Control = new CustomIconStatusControl(loadedTexture, spentTexture, ent.Comp.HorizontalMult);
    }
}
