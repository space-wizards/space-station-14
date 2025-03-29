using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server.Clothing.Systems;

public sealed class PoorlyAttachedSystem : SharedPoorlyAttachedSystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    protected override void Detach(Entity<PoorlyAttachedComponent> entity)
    {
        base.Detach(entity);

        // Make sure the item is equipped in a valid slot (not just in a pocket)
        if (!TryComp<ClothingComponent>(entity, out var clothing) || (clothing.InSlotFlag & clothing.Slots) == SlotFlags.NONE)
            return;

        // Make sure the item is actually equipped (and get the wearer's uid)
        if (!Container.TryGetContainingContainer((entity, null), out var container))
            return;

        var wearer = container.Owner;
        // Base the throw direction off of the wearer's current velocity.
        var velocity = TryComp<PhysicsComponent>(wearer, out var physics) ? physics.LinearVelocity * ThrowSpeedMult : Vector2.Zero;
        // Rotate the throw direction by a randomized amount.
        velocity = Angle.FromDegrees(_random.NextFloat(-ThrowSpread, ThrowSpread)).RotateVec(velocity);

        // Make sure there's nothing stopping the item from being thrown.
        if (!_actionBlocker.CanThrow(wearer, entity))
            return;

        var message = Loc.GetString(entity.Comp.DetachPopup, ("entity", entity.Owner));
        Popup.PopupEntity(message, entity, wearer, PopupType.SmallCaution);

        _transform.SetCoordinates(entity, Transform(wearer).Coordinates);
        _transform.AttachToGridOrMap(entity);
        _throwing.TryThrow(entity, velocity);
    }
}
