using Content.Shared.Examine;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared.Weapons.Ranged.Systems;


public sealed class GunSpreadModifierSystem: EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GunSpreadModifierComponent, GunGetAmmoSpreadEvent>(OnGunGetAmmoSpread);
        SubscribeLocalEvent<GunSpreadModifierComponent, ExaminedEvent>(OnExamine);
    }

    private void OnGunGetAmmoSpread(EntityUid uid, GunSpreadModifierComponent comp, ref GunGetAmmoSpreadEvent args)
    {
        args.Spread *= comp.Spread;
    }

    private void OnExamine(EntityUid uid, GunSpreadModifierComponent comp, ExaminedEvent args)
    {
        var percentage = Math.Round(comp.Spread * 100);
        var loc = percentage < 100 ? "examine-gun-spread-modifier-reduction" : "examine-gun-spread-modifier-increase";
        percentage = percentage < 100 ? 100 - percentage : percentage - 100;
        var msg = Loc.GetString(loc, ("percentage", percentage));
        args.PushMarkup(msg);
    }
}
