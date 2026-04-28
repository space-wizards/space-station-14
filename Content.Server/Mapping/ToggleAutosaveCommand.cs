using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server.Mapping;

[AdminCommand(AdminFlags.Server | AdminFlags.Mapping)]
public sealed class ToggleAutosaveCommand : LocalizedEntityCommands
{
    [Dependency] private readonly MappingSystem _mappingSystem = default!;

    public override string Command => "toggleautosave";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1 && args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!int.TryParse(args[0], out var intMapId))
        {
            shell.WriteError(Loc.GetString("cmd-mapping-failure-integer", ("arg", args[0])));
            return;
        }

        string? path = null;
        if (args.Length == 2)
        {
            path = args[1];
        }

        var mapId = new MapId(intMapId);
        _mappingSystem.ToggleAutosave(mapId, path);
    }
}
