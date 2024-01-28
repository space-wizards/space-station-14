using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Client.Commands;

[AnyCommand]
public sealed class ToggleOutlineCommand : LocalizedCommands
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    public override string Command => "toggleoutline";

    public override string Help => LocalizationManager.GetString($"cmd-{Command}-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var cvar = CCVars.OutlineEnabled;
        var old = _configurationManager.GetCVar(cvar);

        _configurationManager.SetCVar(cvar, !old);
        shell.WriteLine(LocalizationManager.GetString($"cmd-{Command}-notify", ("state", _configurationManager.GetCVar(cvar))));
    }
}
