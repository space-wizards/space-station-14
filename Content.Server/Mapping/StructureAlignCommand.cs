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
        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
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

        var sat = _entManager.System<SharedStructureAlignerSystem>();

        var response = sat.AlignAll(map);
        if (!string.IsNullOrEmpty(response))
            shell.WriteLine(response);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(CompletionHelper.MapIds(_entManager), Loc.GetString("cmd-hint-align-id"));

        return CompletionResult.Empty;
    }
}
