using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Random;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Weapons.Melee.WeaponRandom;

/// <summary>
/// This adds a random damage bonus to melee attacks based on damage bonus amount and probability.
/// </summary>
public sealed class WeaponRandomSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WeaponRandomComponent, MeleeHitEvent>(OnMeleeHit);
    }
    /// <summary>
    /// On Melee hit there is a possible chance of additional bonus damage occuring.
    /// </summary>
    private void OnMeleeHit(EntityUid uid, WeaponRandomComponent component, MeleeHitEvent args)
    {
        if (_random.Prob(component.RandomDamageChance))
        {
            _audio.PlayPvs(component.DamageSound, uid);
            args.BonusDamage = component.DamageBonus;
        }
    }
}
