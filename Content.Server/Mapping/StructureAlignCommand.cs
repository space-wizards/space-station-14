using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Mapping;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server.Mapping;

[AdminCommand(AdminFlags.Mapping)]
public sealed partial class StructureAlignCommand : LocalizedEntityCommands
{
    [Dependency] private IEntityManager _entManager = default!;

    public override string Command => "align";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 2)
        {
            shell.WriteError(Loc.GetString("shell-need-between-arguments", ("lower", 0), ("upper", 2)));
            return;
        }

        MapId? map = null;

        if (args.Length > 0)
        {
            if (!int.TryParse(args[0], out var intMapId))
            {
                shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
                return;
            }

            map = new MapId(intMapId);
        }

        var dry = false;
        if (args.Length > 1)
        {
            if (!bool.TryParse(args[1], out dry))
            {
                shell.WriteError(Loc.GetString("shell-invalid-bool-value", ("value", args[1])));
                return;
            }
        }

        var sat = _entManager.System<SharedStructureAlignerSystem>();

        var response = sat.AlignAll(map, dry);
        if (!string.IsNullOrEmpty(response))
            shell.WriteLine(response);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.MapIds(_entManager),
                Loc.GetString("cmd-align-hint-id")),
            2 => CompletionResult.FromHintOptions(["false", "true"], Loc.GetString("cmd-align-hint-dry")),
            _ => CompletionResult.Empty
        };
    }
}
