using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.MapEditor.Commands;

/// <summary>
///     Moves an entity by setting explicit EntityCoordinates (parent + local position).
///     Stores the parent UID to ensure undo restores the entity to the correct parent,
///     even if the move caused re-parenting.
/// </summary>
public sealed class LocalMoveEntityCommand : IEditorCommand
{
    private readonly IEntityManager _em;
    private readonly EntityUid _uid;
    private readonly EntityUid _parentUid;
    private readonly Vector2 _oldLocalPos;
    private readonly Vector2 _newLocalPos;

    public LocalMoveEntityCommand(IEntityManager em, EntityUid uid, Vector2 oldLocalPos, Vector2 newLocalPos)
    {
        _em = em;
        _uid = uid;
        _oldLocalPos = oldLocalPos;
        _newLocalPos = newLocalPos;
        // Store the parent so we can restore the correct coordinate space on undo.
        _parentUid = em.GetComponent<TransformComponent>(uid).ParentUid;
    }

    public void Execute()
    {
        MoveTo(_parentUid, _newLocalPos);
    }

    public void Undo()
    {
        MoveTo(_parentUid, _oldLocalPos);
    }

    private void MoveTo(EntityUid parent, Vector2 localPos)
    {
        if (!_em.EntityExists(_uid))
            return;

        if (_em.TryGetComponent<PhysicsComponent>(_uid, out var phys) && phys.CanCollide)
            _em.System<SharedPhysicsSystem>().SetCanCollide(_uid, false, body: phys);

        // Use SetLocalPositionRotation which explicitly sets position without re-parenting.
        var xform = _em.GetComponent<TransformComponent>(_uid);
        var xformSys = _em.System<SharedTransformSystem>();

        // Ensure correct parent.
        if (xform.ParentUid != parent && _em.EntityExists(parent))
            xformSys.SetParent(_uid, xform, parent);

        // Detach and reattach to force coordinate recalculation.
        xformSys.SetLocalPositionRotation(_uid, localPos, xform.LocalRotation);
    }
}
