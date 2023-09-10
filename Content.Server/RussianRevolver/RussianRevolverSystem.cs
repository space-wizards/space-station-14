using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Content.Shared.Damage;
using Content.Server.Stunnable;

namespace Content.Server.RussianRevolver;

public sealed class RussianRevolverSystem : EntitySystem
{
    [Dependency]
    private readonly SharedGunSystem _gunSystem = default!;
    [Dependency]
    private readonly SharedAudioSystem _audio = default!;
    [Dependency]
    private readonly DamageableSystem _damageable = default!;
    [Dependency]
    private readonly StunSystem _stun = default!;
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
        if (ammoProvider.Chambers[ammoProvider.CurrentIndex] == true)
        {
            _audio.PlayPvs(component.SoundLose, uid);
            ammoProvider.Chambers[ammoProvider.CurrentIndex] = false;
            if (!TryComp(args.User, out DamageableComponent? damageMe))
            {
                return;
            }
            _damageable.TryChangeDamage(args.User, component.RussianRevolverDamage,
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
        _gunSystem.RussianizeRevolver(uid, ammoDrinker);
    }
}
