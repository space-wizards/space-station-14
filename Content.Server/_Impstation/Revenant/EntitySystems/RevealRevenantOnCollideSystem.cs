using Content.Shared.Revenant.EntitySystems;
using Content.Shared.Revenant.Components;
using Content.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics;

namespace Content.Server.Revenant.EntitySystems;

public sealed partial class RevealRevenantOnCollideSystem : SharedRevealRevenantOnCollideSystem
{
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly CollisionWakeSystem _collisionWake = default!;

    private const string FixtureId = "revenantReveal";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevealRevenantOnCollideComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RevealRevenantOnCollideComponent, ComponentShutdown>(OnShutdown);
    }

    private IPhysShape GetOrCreateShape(EntityUid uid, FixturesComponent? fixtures = null)
    {
        if (Resolve(uid, ref fixtures))
            if (fixtures.Fixtures.TryGetValue("fix1", out var fix))
                return fix.Shape;

        return new PhysShapeCircle(0.35f);
    }

    private void OnStartup(EntityUid uid, RevealRevenantOnCollideComponent comp, ComponentStartup args)
    {
        var fixtures = EnsureComp<FixturesComponent>(uid);
        _fixtures.TryCreateFixture(uid,
            GetOrCreateShape(uid, fixtures),
            FixtureId,
            hard: false,
            collisionMask: (int)CollisionGroup.GhostImpassable,
            collisionLayer: (int)CollisionGroup.GhostImpassable,
            manager: fixtures
        );

        // Disable collision wake so that it can trigger collisions even when sitting still
        var collisionWake = EnsureComp<CollisionWakeComponent>(uid);
        _collisionWake.SetEnabled(uid, false, collisionWake);
    }

    private void OnShutdown(EntityUid uid, RevealRevenantOnCollideComponent comp, ComponentShutdown args)
    {
        _fixtures.DestroyFixture(uid, FixtureId);
    }
}