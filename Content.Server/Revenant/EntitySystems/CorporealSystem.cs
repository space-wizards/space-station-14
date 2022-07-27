using Content.Server.Visible;
using Content.Shared.Physics;
using Content.Shared.Revenant;
using Content.Shared.Movement;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.Physics;
using System.Linq;
using Content.Shared.Movement.Systems;

namespace Content.Server.Revenant.EntitySystems;

/// <summary>
/// Makes the revenant solid when the component is applied.
/// Additionally applies a few visual effects.
/// Used for status effect.
/// </summary>
public sealed class CorporealSystem : EntitySystem
{
    [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CorporealComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CorporealComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CorporealComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
    }

    private void OnRefresh(EntityUid uid, CorporealComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.MovementSpeedDebuff, component.MovementSpeedDebuff);
    }
    
    private void OnStartup(EntityUid uid, CorporealComponent component, ComponentStartup args)
    {
        if (TryComp<AppearanceComponent>(uid, out var app))
            app.SetData(RevenantVisuals.Corporeal, true);

        if (TryComp<FixturesComponent>(uid, out var fixtures) && fixtures.FixtureCount >= 1)
        {
            var fixture = fixtures.Fixtures.Values.First();

            fixture.CollisionMask = (int) (CollisionGroup.SmallMobMask | CollisionGroup.GhostImpassable);
            fixture.CollisionLayer = (int) CollisionGroup.SmallMobLayer;
        }

        if (TryComp<VisibilityComponent>(uid, out var visibility))
        {
            _visibilitySystem.RemoveLayer(visibility, (int) VisibilityFlags.Ghost, false);
            _visibilitySystem.AddLayer(visibility, (int) VisibilityFlags.Normal, false);
            _visibilitySystem.RefreshVisibility(visibility);
        }
        _movement.RefreshMovementSpeedModifiers(uid);
    }

    private void OnShutdown(EntityUid uid, CorporealComponent component, ComponentShutdown args)
    {
        if (TryComp<AppearanceComponent>(uid, out var app))
            app.SetData(RevenantVisuals.Corporeal, false);

        if (TryComp<FixturesComponent>(uid, out var fixtures) && fixtures.FixtureCount >= 1)
        {
            var fixture = fixtures.Fixtures.Values.First();

            fixture.CollisionMask = (int) CollisionGroup.GhostImpassable;
            fixture.CollisionLayer = 0;
        }

        if (TryComp<VisibilityComponent>(uid, out var visibility))
        {
            _visibilitySystem.AddLayer(visibility, (int) VisibilityFlags.Ghost, false);
            _visibilitySystem.RemoveLayer(visibility, (int) VisibilityFlags.Normal, false);
            _visibilitySystem.RefreshVisibility(visibility);
        }
    }
}
