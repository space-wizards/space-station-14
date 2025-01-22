using System.Numerics;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.Player;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Client.Movement.Systems;

public sealed class MobCollisionSystem : SharedMobCollisionSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var player = _player.LocalEntity;

        if (!HasComp<MobCollisionComponent>(player) || !TryComp(player, out PhysicsComponent? physics))
            return;

        if (physics.ContactCount == 0)
            return;

        var ourTransform = _physics.GetPhysicsTransform(player.Value);
        var contacts = _physics.GetContacts(player.Value);
        var direction = Vector2.Zero;

        while (contacts.MoveNext(out var contact))
        {
            if (!contact.IsTouching)
                continue;

            var other = contact.OtherEnt(player.Value);

            if (!HasComp<MobCollisionComponent>(other))
                continue;

            // TODO: Get overlap amount
            var otherTransform = _physics.GetRelativePhysicsTransform(ourTransform, other);

            var diff = otherTransform.Position;

            var penDepth = MathF.Max(0f, 0.7f - diff.LengthSquared());

            // Need the push strength proportional to penetration depth.
            direction += penDepth * diff.Normalized() * 1f * frameTime;
        }

        if (direction == Vector2.Zero)
            return;

        RaisePredictiveEvent(new MobCollisionMessage()
        {
            Direction = direction,
        });
    }
}
