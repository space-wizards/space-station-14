using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Client.Commands
{
    [AnyCommand]
    public sealed class ToggleOutlineCommand : IConsoleCommand
    {
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        // ReSharper disable once StringLiteralTypo
        public string Command => "toggleoutline";
        public string Description => Loc.GetString("toggle-outline-command-description");
        public string Help => Loc.GetString("toggle-outline-command-help", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var cvar = CCVars.OutlineEnabled;
            var old = _configurationManager.GetCVar(cvar);

            _configurationManager.SetCVar(cvar, !old);
            shell.WriteLine(Loc.GetString("toggle-outline-command-notify", ("state", _configurationManager.GetCVar(cvar))));
        }
    }
}
