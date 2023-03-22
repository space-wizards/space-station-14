using Content.Shared.Cluwne;
using Content.Shared.Humanoid;
using Content.Server.Polymorph.Systems;
using Content.Shared.Zombies;
using Content.Shared.Interaction.Events;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Content.Server.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;
using Content.Shared.Interaction.Components;
using Content.Server.Abilities.Mime;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Actions;
using Content.Shared.Interaction;
using Content.Server.Nutrition;
using Prometheus.DotNetRuntime;

namespace Content.Server.Cluwne;

public sealed class CluwneBrainSystem : EntitySystem
{
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CluwneBrainComponent, UseInHandEvent>(AfterEat);
    }

    private void AfterEat(EntityUid uid, CluwneBrainComponent component, UseInHandEvent args)
    {

        if (HasComp<HumanoidAppearanceComponent>(args.User)
                && HasComp<CluwneComponent>(args.User)
                && !HasComp<ZombieComponent>(args.User))
        {
            _polymorphSystem.PolymorphEntity(args.User, "ForcedCluwneBeast");
        }

        else if (HasComp<HumanoidAppearanceComponent>(args.User)
                && !HasComp<CluwneComponent>(args.User)
                && !HasComp<ClumsyComponent>(args.User)
                && !HasComp<MimePowersComponent>(args.User)
                && !HasComp<ZombieComponent>(args.User))
        {
            EnsureComp<CluwneComponent>(args.User);
        }

        else
        {
            var damageSpec = new DamageSpecifier(_prototypeManager.Index<DamageGroupPrototype>("Genetic"), 50);
            _damageableSystem.TryChangeDamage(args.User, damageSpec);
        }
    }
}
