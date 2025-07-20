using System.Numerics;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.GameObjects;
using Robust.Shared.Console;

namespace Content.Server.Mapping;

[AdminCommand(AdminFlags.Debug)]
public sealed class NudgeCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    public string Command => "nudge";
    public string Description => "Moves an entity locally.";
    public string Help => "nudge <entity id> <change in x> <change in y>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteLine("Wrong number of arguments.");
            return;
        }

        if (!float.TryParse(args[1], out var DeltaX) || !float.TryParse(args[2], out var DeltaY))
        {
            shell.WriteLine("Invalid X or Y");
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEntity)
            || !_entMan.TryGetEntity(netEntity, out var uid)
            || !_entMan.EntityExists(uid))
        {
            shell.WriteLine($"Invalid entity: {_entMan.ToPrettyString(netEntity)}");
            return;
        }

        if (!_entMan.TryGetComponent(uid, out TransformComponent? xform))
            return; // ...

        if (!_entMan.TrySystem<TransformSystem>(out var XFormSys))
            return; // ...

        var newPosition = xform.LocalPosition + new Vector2(DeltaX, DeltaY);
        XFormSys.SetLocalPosition(uid.Value, newPosition, xform);
    }
}
