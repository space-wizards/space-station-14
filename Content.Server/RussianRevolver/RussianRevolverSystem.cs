using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Content.Shared.Damage;
using Robust.Shared.Containers;
using Robust.Server.GameObjects;

namespace Content.Server.RussianRevolver;

public sealed class RussianRevolverSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RussianRevolverComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<RussianRevolverComponent, AfterInteractEvent>(OnRussianRevolverSelf);
    }
    private void OnRussianRevolverSelf(EntityUid uid, RussianRevolverComponent component, AfterInteractEvent args)
    {
        if (args.Target != args.User)
            return;
        if (!TryComp(uid, out RevolverAmmoProviderComponent? ammoProvider))
        {
            return;
        }
        if (ammoProvider.Chambers[ammoProvider.CurrentIndex] == true ||
            (TryComp(ammoProvider.AmmoSlots[ammoProvider.CurrentIndex], out CartridgeAmmoComponent? cartridge) && !cartridge.Spent))
        {
            _audio.PlayPvs(component.SoundLose, uid);
            ammoProvider.Chambers[ammoProvider.CurrentIndex] = false;
            if (!TryComp(uid, out ContainerManagerComponent? container))
            {
                return;
            }
            if (ammoProvider.AmmoSlots[ammoProvider.CurrentIndex] != null)
            {
                EntityUid? nullet = ammoProvider.AmmoSlots[ammoProvider.CurrentIndex];
                if (nullet != null)
                {
                    EntityUid bullet = (EntityUid) nullet;
                    if (TryComp(bullet, out CartridgeAmmoComponent? spentAmmo))
                    {
                        spentAmmo.Spent = true;
                        _appearance.SetData(bullet, AmmoVisuals.Spent, true);
                        Dirty(bullet, spentAmmo);
                    }
                }

            }
            if (!TryComp(args.User, out DamageableComponent? damageMe))
            {
                return;
            }
            _damageable.TryChangeDamage(args.User, component.damage,
                true, damageable: damageMe, origin: args.User);
        }
        else
        {
            _audio.PlayPvs(component.SoundWin, uid);
        }
        _gunSystem.Cycle(ammoProvider);
    }
    private void OnComponentInit(EntityUid uid, RussianRevolverComponent component, ComponentInit args)
    {
        if (!TryComp(uid, out RevolverAmmoProviderComponent? ammoDrinker))
        {
            return;
        }
        _gunSystem.RouletteifyRevolver(uid, ammoDrinker);
    }
}
