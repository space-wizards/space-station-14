using Robust.Shared.Random;
using Robust.Shared.Audio.Systems;
using Content.Shared.Damage;
using Content.Shared.Cluwne;
using Content.Shared.Clumsy;
using Content.Shared.Interaction.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mindshield.Components;
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
                    && _random.Prob(GenerateHitChance(entity, component))
                    && !HasComp<ClumsyComponent>(entity)
                    && !HasComp<ZombieComponent>(entity) 
                    && !HasComp<MindShieldComponent>(entity))
                {
                    _audio.PlayPvs(component.InfectionSound, uid);
                    EnsureComp<CluwneComponent>(entity);
                }
            }
        }
    }
    
    private float GenerateHitChance(EntityUid enemy, InfectOnMeleeComponent component)
    {
        float chance = component.InfectionChance;
        if (TryComp<DamageableComponent>(enemy, out var damage))
        {
            var totalDamage = damage.TotalDamage;

            var additionalChance = totalDamage * 0.01f;

            var finalChance = Math.Clamp(chance + additionalChance.Float(), 0f, 1f);

            chance = finalChance;
        }
        
        return chance;
    }
}
