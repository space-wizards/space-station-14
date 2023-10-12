using Robust.Shared.Random;
using Content.Shared.Cluwne;
using Content.Shared.Interaction.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Zombies;
using Content.Shared.Weapons.Melee;

namespace Content.Server.Weapons.Melee.InfectOnMelee;

public sealed class InfectOnMeleeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InfectOnMeleeComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(EntityUid uid, InfectOnMeleeComponent component, MeleeHitEvent args)
    {
        if (component.Cluwinification == true)
        {
            foreach (var entity in args.HitEntities)
            {
                if (HasComp<HumanoidAppearanceComponent>(entity)
                && !_mob.IsDead(entity)
                && _random.Prob(component.InfectionChance)
                && !HasComp<ClumsyComponent>(entity)
                && !HasComp<ZombieComponent>(entity))
                {
                    _audio.PlayPvs(component.InfectionSound, uid);
                    EnsureComp<CluwneComponent>(entity);
                }
            }
        }
    }
}
