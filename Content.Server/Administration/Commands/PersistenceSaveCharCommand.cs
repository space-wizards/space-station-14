using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Server)]
public sealed class PersistenceSaveChar : LocalizedEntityCommands
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;

    public override string Command => "persistencesavechar";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1 || args.Length > 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!int.TryParse(args[0], out var intMapId))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-integer", ("arg", args[0])));
            return;
        }

        var entId = new EntityUid(intMapId);


        var saveFilePath = (args.Length > 1 ? args[1] : null) ?? _config.GetCVar(CCVars.GameMap);
        if (string.IsNullOrWhiteSpace(saveFilePath))
        {
            shell.WriteError(Loc.GetString("cmd-persistencesave-no-path", ("cvar", nameof(CCVars.GameMap))));
            return;
        }

        bool save_stat = _mapLoader.TrySaveGeneric(entId, new ResPath(saveFilePath), out var category);
        shell.WriteLine(Loc.GetString("Did the map save? ") + $"{save_stat}");
    }
}
