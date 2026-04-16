using Robust.Shared.GameObjects;

namespace Content.MapEditor.Commands;

/// <summary>
///     Tracks a spawned entity for undo support. The entity is already spawned before
///     this command is created undo deletes it. Redo is not supported (UID is gone).
/// </summary>
public sealed class SpawnEntityCommand : IEditorCommand
{
    private readonly IEntityManager _em;
    private readonly EntityUid _uid;

    public SpawnEntityCommand(IEntityManager em, EntityUid uid)
    {
        _em = em;
        _uid = uid;
    }

    public void Execute()
    {
        // Entity was already spawned. This exists only for the redo path,
        // which we don't fully support yet (UID is gone after delete).
    }

    public void Undo()
    {
        if (_em.EntityExists(_uid))
            _em.DeleteEntity(_uid);
    }
}
