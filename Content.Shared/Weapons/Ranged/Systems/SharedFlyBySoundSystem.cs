using Content.Shared.Physics;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract class SharedFlyBySoundSystem : EntitySystem
{
    [Dependency] private readonly FixtureSystem _fixtures = default!;

    public const string FlyByFixture = "fly-by";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FlyBySoundComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<FlyBySoundComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, FlyBySoundComponent component, ComponentStartup args)
    {
        if (!TryComp<PhysicsComponent>(uid, out var body)) return;

        var shape = new PhysShapeCircle()
        {
            Radius = component.Range,
        };

        var fixture = new Fixture(body, shape)
        {
            Hard = false,
            ID = FlyByFixture,
            CollisionLayer = (int) CollisionGroup.MobMask,
        };

        _fixtures.TryCreateFixture(body, fixture);
    }

    private void OnShutdown(EntityUid uid, FlyBySoundComponent component, ComponentShutdown args)
    {
        if (!TryComp<PhysicsComponent>(uid, out var body) ||
            MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating)
        {
            return;
        }

        _fixtures.DestroyFixture(body, FlyByFixture);
    }
}
