using Content.Server.Hands.Components;
using Content.Shared.Standing;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Server.Standing;

public class StandingStateSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private void FallOver(EntityUid uid, StandingStateComponent component, DropHandItemsEvent args)
    {
        var direction = EntityManager.TryGetComponent(uid, out PhysicsComponent? comp) ? comp.LinearVelocity / 50 : Vector2.Zero;
        var dropAngle = _random.NextFloat(0.8f, 1.2f);

        if (EntityManager.TryGetComponent(uid, out HandsComponent? hands))
        {
            foreach (var heldItem in hands.GetAllHeldItems())
            {
                if (hands.Drop(heldItem.Owner))
                {
                    Throwing.ThrowHelper.TryThrow(heldItem.Owner,
                        _random.NextAngle().RotateVec(direction / dropAngle +
                                                      EntityManager.GetEntity(uid).Transform.WorldRotation.ToVec() / 50),
                        0.5f * dropAngle * _random.NextFloat(-0.9f, 1.1f),
                        EntityManager.GetEntity(uid), 0);
                }
            }
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StandingStateComponent, DropHandItemsEvent>(FallOver);
    }

}
