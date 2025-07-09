using Content.Shared._Starlight.Weapon.Components;
using Content.Shared.Examine;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Random;

namespace Content.Shared.Starlight.Weapon.Systems;

public abstract partial class SharedWeaponDismantleOnShootSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeaponDismantleOnShootComponent, ExaminedEvent>(OnExamine);
    }

    public bool DismantleCheck(Entity<WeaponDismantleOnShootComponent> ent, ref AmmoShotEvent args)
    {
        //roll to see if we explode or not
        var random = IoCManager.Resolve<IRobustRandom>();
        //1.0f means always true, 0.0f means always false
        if (!random.Prob(ent.Comp.DismantleChance))
            return false;

        return true;
    }

    private void OnExamine(Entity<WeaponDismantleOnShootComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.DismantleChance <= 0.0f)
            return;

        args.PushMarkup(Loc.GetString("examine-weapon-dismantle-on-shoot", ("chance", MathF.Round(ent.Comp.DismantleChance * 100, 1))));
    }
}
