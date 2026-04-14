using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.MapEditor.Commands;

/// <summary>
///     Rotates an entity by the given angle delta. Supports undo by reversing the rotation.
/// </summary>
public sealed class RotateEntityCommand : IEditorCommand
{
    private readonly IEntityManager _em;
    private readonly EntityUid _uid;
    private readonly Angle _delta;

    public RotateEntityCommand(IEntityManager em, EntityUid uid, Angle delta)
    {
        _em = em;
        _uid = uid;
        _delta = delta;
    }

    public void Execute()
    {
        if (!_em.EntityExists(_uid))
            return;

        var xform = _em.GetComponent<TransformComponent>(_uid);
        xform.LocalRotation += _delta;
    }

    public void Undo()
    {
        if (!_em.EntityExists(_uid))
            return;

        var xform = _em.GetComponent<TransformComponent>(_uid);
        xform.LocalRotation -= _delta;
    }
}
