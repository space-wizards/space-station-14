using System.Linq;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Revenant.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Revenant.Systems;

/// <summary>
///     Makes the revenant solid when the component is applied.
///     Additionally, applies a few visual effects.
///     Used for status effect.
/// </summary>
public abstract class SharedCorporealSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CorporealComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CorporealComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CorporealComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
    }

    private void OnRefresh(Entity<CorporealComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.MovementSpeedDebuff, ent.Comp.MovementSpeedDebuff);
    }

    public virtual void OnStartup(Entity<CorporealComponent> ent, ref ComponentStartup args)
    {
        _appearance.SetData(ent, RevenantVisuals.Corporeal, true);
        UpdateCollision(ent, true);
        _movement.RefreshMovementSpeedModifiers(ent);
    }

    public virtual void OnShutdown(Entity<CorporealComponent> ent, ref ComponentShutdown args)
    {
        _appearance.SetData(ent, RevenantVisuals.Corporeal, false);
        UpdateCollision(ent, false);
        ent.Comp.MovementSpeedDebuff = 1; //just so we can avoid annoying code elsewhere
        _movement.RefreshMovementSpeedModifiers(ent);
    }

    private void UpdateCollision(EntityUid uid, bool isCorporeal)
    {
        if (!TryComp<FixturesComponent>(uid, out var fixtures) || fixtures.FixtureCount == 0)
            return;

        var fixture = fixtures.Fixtures.First();

        if (isCorporeal)
        {
            _physics.SetCollisionMask(uid,
                fixture.Key,
                fixture.Value,
                (int)(CollisionGroup.SmallMobMask | CollisionGroup.GhostImpassable),
                fixtures);
            _physics.SetCollisionLayer(uid,
                fixture.Key,
                fixture.Value,
                (int)CollisionGroup.SmallMobLayer,
                fixtures);
        }
        else
        {
            _physics.SetCollisionMask(uid,
                fixture.Key,
                fixture.Value,
                (int)CollisionGroup.None,
                fixtures);
            _physics.SetCollisionLayer(uid,
                fixture.Key,
                fixture.Value,
                (int)CollisionGroup.GhostImpassable,
                fixtures);
        }
    }
}
