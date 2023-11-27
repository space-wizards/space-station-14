using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Random;
using Content.Shared.Cluwne;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Weapons.Melee.WeaponRandom;

public sealed class WeaponRandomSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WeaponRandomComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(EntityUid uid, WeaponRandomComponent component, MeleeHitEvent args)
    {
        foreach (var entity in args.HitEntities)
        {
            if (HasComp<CluwneComponent>(entity) && component.AntiCluwne)
            {
                _audio.PlayPvs(component.DamageSound, uid);
                args.BonusDamage = component.DamageBonus;
            }

            else if (_random.Prob(component.RandomDamageChance) && component.RandomDamage)
            {
                _audio.PlayPvs(component.DamageSound, uid);
                args.BonusDamage = component.DamageBonus;
            }
        }
    }
}
