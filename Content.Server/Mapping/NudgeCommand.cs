using System.Numerics;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Physics;

namespace Content.Server.Mapping;

public sealed class NudgeCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    public override string Command => "nudge";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!float.TryParse(args[1], out var DeltaX))
        {
            shell.WriteLine(Loc.GetString("shell-argument-number-invalid", ("index", 2)));
            return;
        }

        if (!float.TryParse(args[2], out var DeltaY))
        {
            shell.WriteLine(Loc.GetString("shell-argument-number-invalid", ("index", 3)));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEntity)
            || !_entMan.TryGetEntity(netEntity, out var uid)
            || !_entMan.EntityExists(uid))
        {
            shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
            return;
        }

        if (!_entMan.TryGetComponent(uid, out TransformComponent? xform))
        {
            shell.WriteLine(Loc.GetString("shell-entity-target-lacks-component", ("componentName", nameof(TransformComponent))));
            return;
        }

        var newPosition = xform.LocalPosition + new Vector2(DeltaX, DeltaY);
        _transform.SetLocalPosition(uid.Value, newPosition, xform);
    }
}
