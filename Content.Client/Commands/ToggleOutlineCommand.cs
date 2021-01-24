using Content.Shared;
using Robust.Client.Console;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;

namespace Content.Client.Commands
{
    public class ToggleOutlineCommand : IClientCommand
    {
        public string Command => "toggleoutline";

        public string Description => "Toggles outline drawing on entities.";

        public string Help => "";

        public bool Execute(IClientConsoleShell shell, string argStr, string[] args)
        {
            var configurationManager = IoCManager.Resolve<IConfigurationManager>();
            var cvar = CCVars.OutlineEnabled;
            var old = configurationManager.GetCVar(cvar);

            configurationManager.SetCVar(cvar, !old);
            shell.WriteLine($"Draw outlines set to: {configurationManager.GetCVar(cvar)}");

            return false;
        }
    }
}
