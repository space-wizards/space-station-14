using Content.Server.NPC.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Server.Movement.Systems;

<<<<<<< HEAD
/// <inheritdoc/>
public sealed partial class SpriteMovementSystem : SharedSpriteMovementSystem
{
    [SubscribeLocalEvent]
    private void OnNPCMove(Entity<SpriteMovementComponent> ent, ref NPCMoveEvent args)
    {
        ent.Comp.IsMoving = args.Direction.IsLongerThan(float.Epsilon);
        Dirty(ent);
    }
}
=======
public sealed partial class SpriteMovementSystem : SharedSpriteMovementSystem;
>>>>>>> upstream/master
