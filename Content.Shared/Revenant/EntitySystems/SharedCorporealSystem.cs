using System.Linq;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Revenant.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Revenant.EntitySystems;

/// <summary>
/// Manages the shared aspects of the corporeal state, collision and movement speed changes.
/// </summary>
public abstract class SharedCorporealSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CorporealComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<CorporealComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CorporealComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
    }

    private void OnRefresh(EntityUid uid, CorporealComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.MovementSpeedDebuff, component.MovementSpeedDebuff);
    }

    protected virtual void OnInit(EntityUid uid, CorporealComponent component, ComponentStartup args)
    {
        _appearance.SetData(uid, RevenantVisuals.Corporeal, true);
        UpdateCollision(uid, true);
        _movement.RefreshMovementSpeedModifiers(uid);
    }

    protected virtual void OnShutdown(EntityUid uid, CorporealComponent component, ComponentShutdown args)
    {
        _appearance.SetData(uid, RevenantVisuals.Corporeal, false);
        UpdateCollision(uid, false);
        component.MovementSpeedDebuff = 1f;
        _movement.RefreshMovementSpeedModifiers(uid);
    }

    private void UpdateCollision(EntityUid uid, bool isCorporeal)
    {
        if (!TryComp<FixturesComponent>(uid, out var fixtures) || fixtures.FixtureCount == 0)
            return;

        var fixture = fixtures.Fixtures.First();

        if (isCorporeal)
        {
            _physics.SetCollisionMask(uid, fixture.Key, fixture.Value,
                (int)(CollisionGroup.SmallMobMask | CollisionGroup.GhostImpassable), fixtures);
            _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value,
                (int)CollisionGroup.SmallMobLayer, fixtures);
        }
        else
        {
            _physics.SetCollisionMask(uid, fixture.Key, fixture.Value,
                (int)CollisionGroup.None, fixtures);
            _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value,
                (int)CollisionGroup.GhostImpassable, fixtures);
        }
    }
}
