using System.Numerics;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Physics;
using Robust.Shared.Toolshed;

namespace Content.Server.Mapping;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class NudgeCommand : ToolshedCommand
{
    private TransformSystem? _transform;

    [CommandImplementation]
    private void Nudge(EntityUid uid, [PipedArgument] Vector2 delta)
    {
        _transform ??= GetSys<TransformSystem>();

        var xform = Transform(uid);

        var newPosition = xform.LocalPosition + delta;
        _transform.SetLocalPosition(uid, newPosition, xform);
    }

    [CommandImplementation]
    public void Nudge([PipedArgument] EntityUid uid, float deltaX, float deltaY)
        => Nudge(uid, new Vector2(deltaX, deltaY));

    [CommandImplementation]
    public void Nudge([PipedArgument] IEnumerable<EntityUid> input, float deltaX, float deltaY)
    {
        foreach (var entityUid in input)
        {
            Nudge(entityUid, deltaX, deltaY);
        }
    }

    [CommandImplementation]
    public void Nudge(int entity_id, float deltaX, float deltaY)
    {
        if (!NetEntity.TryParse(entity_id.ToString(), out var netEntity)
            || !EntityManager.TryGetEntity(netEntity, out var uid)
            || !EntityManager.EntityExists(uid))
        {
            return;
        }

        Nudge(uid.Value, deltaX, deltaY);
    }
}
