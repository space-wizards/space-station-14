using System.Numerics;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server.Clothing.Systems;

public sealed class PoorlyAttachedSystem : SharedPoorlyAttachedSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    private const float DetachedItemSpread = 45;
    private const float DetachedItemBaseSpeed = 5;

    protected override void Throw(Entity<PoorlyAttachedComponent> entity, EntityUid wearer)
    {
        var wearerVelocity = TryComp<PhysicsComponent>(wearer, out var wearerPhysics) ? wearerPhysics.LinearVelocity : Vector2.Zero;
        var spreadMaxAngle = Angle.FromDegrees(DetachedItemSpread);

        // Rotate the item's throw vector a bit for each item
        var angleOffset = _random.NextAngle(-spreadMaxAngle, spreadMaxAngle);
        // Rotate the wearer's velocity vector by the angle offset to get the item's velocity vector
        var itemVelocity = angleOffset.RotateVec(wearerVelocity);
        // Decrease the distance of the throw by a random amount
        itemVelocity *= _random.NextFloat(1f);
        // Heavier objects don't get thrown as far
        // If the item doesn't have a physics component, it isn't going to get thrown anyway, but we'll assume infinite mass
        itemVelocity *= TryComp<PhysicsComponent>(entity, out var itemPhysics) ? itemPhysics.InvMass : 0;
        // Vary the speed a little to make it look more interesting
        var throwSpeed = DetachedItemBaseSpeed * _random.NextFloat(0.9f, 1.1f);

        var message = Loc.GetString(entity.Comp.DetachPopup, ("entity", entity.Owner));
        Popup.PopupEntity(message, entity, wearer, PopupType.SmallCaution);

        _transform.DropNextTo(entity.Owner, wearer);

        _throwing.TryThrow(entity,
            itemVelocity,
            throwSpeed,
            entity,
            pushbackRatio: 0,
            compensateFriction: false
        );
    }
}
