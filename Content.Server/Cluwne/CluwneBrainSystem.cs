using Content.Shared.Cluwne;
using Content.Shared.Humanoid;
using Content.Server.Polymorph.Systems;
using Content.Shared.Zombies;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Content.Shared.Interaction.Components;
using Content.Server.Disease;

namespace Content.Server.Cluwne;

public sealed class CluwneBrainSystem : EntitySystem
{
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DiseaseSystem _disease = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CluwneBrainComponent, ComponentStartup>(AfterEat);
    }

    private void AfterEat(EntityUid uid, CluwneBrainComponent component, ComponentStartup args)
    {
        if (HasComp<HumanoidAppearanceComponent>(uid)
        && HasComp<CluwneComponent>(uid)
        && !HasComp<ZombieComponent>(uid))
        {
            _polymorph.PolymorphEntity(uid, "ForcedCluwneBeast");
            _disease.CureAllDiseases(uid);
            RemComp<CluwneBrainComponent>(uid);
        }

        else if (HasComp<HumanoidAppearanceComponent>(uid)
        && !HasComp<CluwneComponent>(uid)
        && !HasComp<ClumsyComponent>(uid)
        && !HasComp<ZombieComponent>(uid))
        {
            EnsureComp<CluwneComponent>(uid);
            _disease.CureAllDiseases(uid);
            RemComp<CluwneBrainComponent>(uid);
        }

        else
        {
            var damageSpec = new DamageSpecifier(_proto.Index<DamageGroupPrototype>("Genetic"), 50);
            _damageable.TryChangeDamage(uid, damageSpec);
        }
    }
}
