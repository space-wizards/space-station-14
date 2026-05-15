using System.Linq;
using Content.Shared.Corporeal.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Corporeal.Systems;

/// <summary>
/// Makes the revenant solid when the component is applied.
/// Additionally applies a few visual effects.
/// </summary>
public abstract partial class SharedCorporealSystem : EntitySystem
{
    [Dependency] private SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CorporealStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<CorporealStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
    }

    protected virtual void OnApplied(Entity<CorporealStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (TryComp<FixturesComponent>(args.Target, out var fixtures) && fixtures.FixtureCount >= 1)
        {
            var fixture = fixtures.Fixtures.First();

            _physics.SetCollisionMask(args.Target, fixture.Key, fixture.Value, ent.Comp.CollisionMask, fixtures);
            _physics.SetCollisionLayer(args.Target, fixture.Key, fixture.Value, ent.Comp.CollisionLayer, fixtures);
        }
    }

    protected virtual void OnRemoved(Entity<CorporealStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (TryComp<FixturesComponent>(args.Target, out var fixtures) && fixtures.FixtureCount >= 1)
        {
            var fixture = fixtures.Fixtures.First();

            _physics.SetCollisionMask(args.Target, fixture.Key, fixture.Value, ent.Comp.RemovedCollisionMask, fixtures);
            _physics.SetCollisionLayer(args.Target, fixture.Key, fixture.Value, ent.Comp.RemovedCollisionLayer, fixtures);
        }
    }
}
