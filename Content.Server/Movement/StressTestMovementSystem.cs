using System.Numerics;
using Content.Server.Movement.Components;

namespace Content.Server.Movement;

public sealed class StressTestMovementSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StressTestMovementComponent, ComponentStartup>(OnStressStartup);
    }

    private void OnStressStartup(EntityUid uid, StressTestMovementComponent component, ComponentStartup args)
    {
        component.Origin = _transform.GetWorldPosition(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StressTestMovementComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var stressTest, out var transform))
        {
            if (!transform.ParentUid.IsValid())
                continue;

            stressTest.Progress += frameTime;

            if (stressTest.Progress > 1)
            {
                stressTest.Progress -= 1;
            }

            var x = MathF.Sin(stressTest.Progress * MathHelper.TwoPi);
            var y = MathF.Cos(stressTest.Progress * MathHelper.TwoPi);

            _transform.SetWorldPosition(transform, stressTest.Origin + new Vector2(x, y) * 5);
        }
    }
}
