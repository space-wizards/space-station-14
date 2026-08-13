using Content.Shared.Administration;
using Content.Shared.Mapping;
using Content.Shared.Maps;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed partial class SatanCommand : IConsoleCommand
{
    [Dependency] private IEntityManager _entManager = default!;

    public string Command => "satan";

    public string Description => Loc.GetString("satan-command-description");

    public string Help => Loc.GetString("satan-command-help-text");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
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
                shell.WriteError(Loc.GetString("cmd-mapping-failure-integer", ("arg", args[0])));
                return;
            }

            map = new MapId(intMapId);
        }


        var sat = _entManager.System<SharedSatanSystem>();

        var response = sat.AlignAll(map);
        if (!string.IsNullOrEmpty(response))
            shell.WriteLine(response);
    }
}
