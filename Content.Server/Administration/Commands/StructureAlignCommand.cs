using Content.Shared.Administration;
using Content.Shared.Mapping;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed partial class StructureAlignCommand : IConsoleCommand
{
    [Dependency] private IEntityManager _entManager = default!;

    public string Command => "align";

    public string Description => Loc.GetString("satan-command-description");

    public string Help => Loc.GetString("satan-command-help-text");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        //TODO:ERRANT require a safety when in Release conf?

        MapId? map = null;

        if (args.Length > 0)
        {
            if (!int.TryParse(args[0], out var intMapId))
            {
                shell.WriteError(Loc.GetString("cmd-mapping-failure-integer", ("arg", args[0])));
                return;
            }

            map = new MapId(intMapId);
        }


        var sat = _entManager.System<SharedStructureAlignerSystem>();

        var response = sat.AlignAll(map);
        if (!string.IsNullOrEmpty(response))
            shell.WriteLine(response);
    }
}
