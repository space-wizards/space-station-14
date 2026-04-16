using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.MapEditor.Commands;

/// <summary>
///     Moves an entity from one position to another. Supports undo by restoring the original coordinates.
///     Temporarily disables physics collision during the move to prevent broadphase crashes.
/// </summary>
public sealed class MoveEntityCommand : IEditorCommand
{
    private readonly IEntityManager _em;
    private readonly EntityUid _uid;
    private readonly EntityCoordinates _oldCoords;
    private readonly EntityCoordinates _newCoords;

    public MoveEntityCommand(IEntityManager em, EntityUid uid, EntityCoordinates oldCoords, EntityCoordinates newCoords)
    {
        _em = em;
        _uid = uid;
        _oldCoords = oldCoords;
        _newCoords = newCoords;
    }

    public void Execute()
    {
        MoveEntity(_newCoords);
    }

    public void Undo()
    {
        MoveEntity(_oldCoords);
    }

    private void MoveEntity(EntityCoordinates target)
    {
        if (!_em.EntityExists(_uid))
            return;

        // Disable collision before moving to avoid physics broadphase assertion crashes.
        var hadCollision = false;
        if (_em.TryGetComponent<PhysicsComponent>(_uid, out var physics))
        {
            hadCollision = physics.CanCollide;
            if (hadCollision)
                _em.System<SharedPhysicsSystem>().SetCanCollide(_uid, false, body: physics);
        }

        _em.System<SharedTransformSystem>().SetCoordinates(_uid, target);

        // Restore collision.
        if (hadCollision && _em.TryGetComponent<PhysicsComponent>(_uid, out physics))
            _em.System<SharedPhysicsSystem>().SetCanCollide(_uid, true, body: physics);
    }
}
