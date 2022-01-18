using System;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.Examine;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Weapon.Ranged;

public sealed partial class GunSystem
{
    private void OnGunExamine(EntityUid uid, ServerRangedWeaponComponent component, ExaminedEvent args)
    {
        var fireRateMessage = Loc.GetString(component.FireRateSelector switch
        {
            FireRateSelector.Safety => "server-ranged-barrel-component-on-examine-fire-rate-safety-description",
            FireRateSelector.Single => "server-ranged-barrel-component-on-examine-fire-rate-single-description",
            FireRateSelector.Automatic => "server-ranged-barrel-component-on-examine-fire-rate-automatic-description",
            _ => throw new IndexOutOfRangeException()
        });

        args.PushText(fireRateMessage);
    }

    private void OnBoltExamine(EntityUid uid, BoltActionBarrelComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("bolt-action-barrel-component-on-examine", ("caliber", component.Caliber)));
    }

    private void OnPumpExamine(EntityUid uid, PumpBarrelComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("pump-barrel-component-on-examine", ("caliber", component.Caliber)));
    }

    private void OnMagazineExamine(EntityUid uid, ServerMagazineBarrelComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("server-magazine-barrel-component-on-examine", ("caliber", component.Caliber)));

        foreach (var magazineType in component.GetMagazineTypes())
        {
            args.PushMarkup(Loc.GetString("server-magazine-barrel-component-on-examine-magazine-type", ("magazineType", magazineType)));
        }
    }
}
