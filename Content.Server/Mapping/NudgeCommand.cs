using System.Numerics;
using Content.Server.Administration;
using Content.Shared.Administration;
using NetCord;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Physics;
using Robust.Shared.Toolshed;

namespace Content.Server.Mapping;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class NudgeCommand : ToolshedCommand
{
    private TransformSystem? _transform;

    private void Nudge(EntityUid uid, float deltaX, float deltaY)
    {
        _transform ??= GetSys<TransformSystem>();

        var xform = Transform(uid);

        _transform.SetLocalPosition(uid, xform.LocalPosition + new Vector2(deltaX, deltaY), xform);
    }

    [CommandImplementation("up")]
    void NudgeUp(IEnumerable<EntityUid> input, float deltaY) => Nudge(input, 0, deltaY);
    [CommandImplementation("up")]
    void NudgeUpPiped([PipedArgument] IEnumerable<EntityUid> input, float deltaY) => Nudge(input, 0, deltaY);
    [CommandImplementation("right")]
    void NudgeRight(IEnumerable<EntityUid> input, float deltaX) => Nudge(input, deltaX, 0);
    [CommandImplementation("right")]
    void NudgeRightPiped([PipedArgument] IEnumerable<EntityUid> input, float deltaX) => Nudge(input, deltaX, 0);

    [CommandImplementation("xy")]
    public void Nudge([PipedArgument] IEnumerable<EntityUid> input, float deltaX, float deltaY)
    {
        foreach (var entityUid in input)
        {
            Nudge(entityUid, deltaX, deltaY);
        }
    }

    [CommandImplementation("xy")]
    public void Nudge(int entity, float deltaX, float deltaY)
    {
        if (!NetEntity.TryParse(entity.ToString(), out var netEntity)
            || !EntityManager.TryGetEntity(netEntity, out var uid)
            || !EntityManager.EntityExists(uid))
        {
            throw new EntityNotFoundException();
        }

        Nudge(uid.Value, deltaX, deltaY);
    }
}
