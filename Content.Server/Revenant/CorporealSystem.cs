using Content.Shared.Physics;
using Content.Shared.Revenant;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using System.Linq;

namespace Content.Server.Revenant;

/// <summary>
/// Makes the revenant solid when the component is applied.
/// Additionally applies a few visual effects.
/// Used for status effect.
/// </summary>
public sealed class CorporealSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CorporealComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CorporealComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, CorporealComponent component, ComponentStartup args)
    {
        if (TryComp<AppearanceComponent>(uid, out var app))
            app.SetData(RevenantVisuals.Corporeal, true);

        if (!TryComp<FixturesComponent>(uid, out var fixtures) || fixtures.FixtureCount < 1)
            return;

        var fixture = fixtures.Fixtures.Values.First();

        fixture.CollisionMask = (int) (CollisionGroup.SmallMobMask | CollisionGroup.GhostImpassable);
        fixture.CollisionLayer = (int) CollisionGroup.SmallMobLayer;

        var light = EnsureComp<PointLightComponent>(uid);
        light.Color = Color.MediumPurple;
        light.Radius = 1.5f;
        light.Softness = 0.75f;
    }

    private void OnShutdown(EntityUid uid, CorporealComponent component, ComponentShutdown args)
    {
        if (TryComp<AppearanceComponent>(uid, out var app))
            app.SetData(RevenantVisuals.Corporeal, false);

        if (!TryComp<FixturesComponent>(uid, out var fixtures) || fixtures.FixtureCount < 1)
            return;

        var fixture = fixtures.Fixtures.Values.First();

        fixture.CollisionMask = (int) CollisionGroup.GhostImpassable;
        fixture.CollisionLayer = 0;

        RemComp<PointLightComponent>(uid);
    }
}
