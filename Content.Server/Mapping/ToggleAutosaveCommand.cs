using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
//using Robust.Shared.ContentPack;
using Robust.Shared.Map;

namespace Content.Server.Mapping;

[AdminCommand(AdminFlags.Server | AdminFlags.Mapping)]
public sealed partial class ToggleAutosaveCommand : LocalizedEntityCommands
{
    //[Dependency] private IResourceManager _resourceMgr = default!;
    [Dependency] private MappingSystem _mappingSystem = default!;

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
        shell.WriteLine(_mappingSystem.ToggleAutosave(mapId, path)
            ? Loc.GetString("cmd-toggleautosave-enabled", ("mapId", mapId))
            : Loc.GetString("cmd-toggleautosave-disabled", ("mapId", mapId)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
                return CompletionResult.FromHint(Loc.GetString("cmd-hint-mapping-id"));
            // TODO COMMANDS implement a completion helper that lists all paths starting from a specific folder instead of just user data folder
            // Here it's needed because all auto saves are implicitly located in the /Autosaves folder
            //case 2:
                //var opts = CompletionHelper.UserFilePath(args[1], _resourceMgr.UserData);
                //return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-hint-mapping-path"));
        }
        return CompletionResult.Empty;
    }
}
