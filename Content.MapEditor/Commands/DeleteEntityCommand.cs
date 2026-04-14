using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.MapEditor.Commands;

/// <summary>
///     Deletes an entity for undo support. Stores the prototype ID and coordinates
///     so undo can respawn a replacement (new UID).
/// </summary>
public sealed class DeleteEntityCommand : IEditorCommand
{
    private readonly IEntityManager _em;
    private EntityUid _uid;
    private readonly string? _protoId;
    private readonly EntityCoordinates _coords;

    public DeleteEntityCommand(IEntityManager em, EntityUid uid)
    {
        _em = em;
        _uid = uid;
        var meta = em.GetComponent<MetaDataComponent>(uid);
        _protoId = meta.EntityPrototype?.ID;
        _coords = em.GetComponent<TransformComponent>(uid).Coordinates;
    }

    public void Execute()
    {
        if (_em.EntityExists(_uid))
            _em.DeleteEntity(_uid);
    }

    public void Undo()
    {
        // Respawn a replacement entity (new UID).
        if (_protoId != null)
            _uid = _em.SpawnEntity(_protoId, _coords);
    }
}
