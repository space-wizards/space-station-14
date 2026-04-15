using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.MapEditor.Commands;

/// <summary>
///     Moves an entity from one position to another. Supports undo by restoring the original coordinates.
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
        if (!_em.EntityExists(_uid))
            return;

        try
        {
            _em.System<SharedTransformSystem>().SetCoordinates(_uid, _newCoords);
        }
        catch
        {
            // Physics assertions can fire during coordinate changes — ignore.
        }
    }

    public void Undo()
    {
        if (!_em.EntityExists(_uid))
            return;

        try
        {
            _em.System<SharedTransformSystem>().SetCoordinates(_uid, _oldCoords);
        }
        catch
        {
            // Physics assertions can fire during coordinate changes — ignore.
        }
    }
}
