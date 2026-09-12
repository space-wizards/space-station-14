using Content.Server.NPC.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Server.Movement.Systems;

/// <inheritdoc/>
public sealed partial class ServerSpriteMovementSystem : SpriteMovementSystem
{
    [SubscribeLocalEvent]
    private void OnNPCMove(Entity<SpriteMovementComponent> ent, ref NPCMoveEvent args)
    {
        ent.Comp.IsMoving = args.Direction.IsLongerThan(float.Epsilon);
        Dirty(ent);
    }
}
