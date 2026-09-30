using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Afk;

[AdminCommand(AdminFlags.Server)]
public sealed partial class SetAfkConfirmSoundCommand : LocalizedEntityCommands
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IResourceManager _res = default!;

    public override string Command => "setafkconfirmationsound";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-setafkconfirmationsound-invalid-arguments"));
            return;
        }

        var path = new ResPath(args[0]);
        if (!path.IsRooted)
        {
            shell.WriteError(Loc.GetString("cmd-setafkconfirmationsound-not-rooted"));
            return;
        }

        _cfg.SetCVar(CCVars.AfkConfirmSound, path.ToString());
        shell.WriteLine(Loc.GetString("cmd-setafkconfirmationsound-success", ("path", path.ToString())));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        var options = CompletionHelper.AudioFilePath(args[0], _proto, _res);
        return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-setafkconfirmationsound-hint"));
    }
}
