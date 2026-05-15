using Content.Shared.Physics;
using Robust.Shared.Physics;
using System.Linq;
using Content.Shared.Revenant.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Revenant.EntitySystems;

/// <summary>
/// Makes the revenant solid when the component is applied.
/// Additionally applies a few visual effects.
/// Used for status effect.
/// </summary>
public abstract partial class SharedCorporealSystem : EntitySystem
{
    public static readonly EntProtoId CorporealStatusEffect = "StatusEffectCorporeal";

    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CorporealStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<CorporealStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
    }

    public virtual void OnApplied(Entity<CorporealStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _appearance.SetData(args.Target, RevenantVisuals.Corporeal, true);

        if (TryComp<FixturesComponent>(args.Target, out var fixtures) && fixtures.FixtureCount >= 1)
        {
            var fixture = fixtures.Fixtures.First();

            _physics.SetCollisionMask(args.Target, fixture.Key, fixture.Value, ent.Comp.CollisionMask, fixtures);
            _physics.SetCollisionLayer(args.Target, fixture.Key, fixture.Value, ent.Comp.CollisionLayer, fixtures);
        }
    }

    public virtual void OnRemoved(Entity<CorporealStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _appearance.SetData(args.Target, RevenantVisuals.Corporeal, false);

        if (TryComp<FixturesComponent>(args.Target, out var fixtures) && fixtures.FixtureCount >= 1)
        {
            var fixture = fixtures.Fixtures.First();

            _physics.SetCollisionMask(args.Target, fixture.Key, fixture.Value, ent.Comp.RemovedCollisionMask, fixtures);
            _physics.SetCollisionLayer(args.Target, fixture.Key, fixture.Value, ent.Comp.RemovedCollisionLayer, fixtures);
        }
    }
}
