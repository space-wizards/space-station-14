using Content.Shared.Examine;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed class GunSpreadModifierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GunSpreadModifierComponent, GunGetAmmoSpreadEvent>(OnGunGetAmmoSpread);
        SubscribeLocalEvent<GunSpreadModifierComponent, ExaminedEvent>(OnExamine);
    }

    private void OnGunGetAmmoSpread(Entity<GunSpreadModifierComponent> ent, ref GunGetAmmoSpreadEvent args)
    {
        args.Spread *= ent.Comp.Spread;
    }

    private void OnExamine(Entity<GunSpreadModifierComponent> ent, ref ExaminedEvent args)
    {
        var percentage = Math.Round(ent.Comp.Spread * 100);
        var loc = percentage < 100 ? "examine-gun-spread-modifier-reduction" : "examine-gun-spread-modifier-increase";
        percentage = percentage < 100 ? 100 - percentage : percentage - 100;
        var msg = Loc.GetString(loc, ("percentage", percentage));
        args.PushMarkup(msg);
    }
}
