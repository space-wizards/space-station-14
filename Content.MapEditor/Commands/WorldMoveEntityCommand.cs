using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.MapEditor.Commands;

/// <summary>
///     Moves an entity using world positions (not parent-relative coordinates).
///     This avoids issues with entity re-parenting across undo/redo cycles.
///     Disables physics collision before moving to prevent broadphase crashes.
/// </summary>
public sealed class WorldMoveEntityCommand : IEditorCommand
{
    private readonly IEntityManager _em;
    private readonly EntityUid _uid;
    private readonly Vector2 _oldWorldPos;
    private readonly Vector2 _newWorldPos;

    public WorldMoveEntityCommand(IEntityManager em, EntityUid uid, Vector2 oldWorldPos, Vector2 newWorldPos)
    {
        _em = em;
        _uid = uid;
        _oldWorldPos = oldWorldPos;
        _newWorldPos = newWorldPos;
    }

    public void Execute()
    {
        MoveToWorldPos(_newWorldPos);
    }

    public void Undo()
    {
        MoveToWorldPos(_oldWorldPos);
    }

    private void MoveToWorldPos(Vector2 target)
    {
        if (!_em.EntityExists(_uid))
            return;

        // Disable collision to prevent physics broadphase assertion crashes.
        if (_em.TryGetComponent<PhysicsComponent>(_uid, out var physics) && physics.CanCollide)
            _em.System<SharedPhysicsSystem>().SetCanCollide(_uid, false, body: physics);

        _em.System<SharedTransformSystem>().SetWorldPosition(_uid, target);
    }
}
