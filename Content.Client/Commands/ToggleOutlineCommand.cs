using Content.Shared;
using Robust.Client.Interfaces.Console;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;

namespace Content.Client.Commands
{
    public class ToggleOutlineCommand : IConsoleCommand
    {
        public string Command => "toggleoutline";

        public string Description => "Toggles outline drawing on entities.";

        public string Help => "";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var configurationManager = IoCManager.Resolve<IConfigurationManager>();
            var cvar = CCVars.OutlineEnabled;
            var old = configurationManager.GetCVar(cvar);

            configurationManager.SetCVar(cvar, !old);
            console.AddLine($"Draw outlines set to: {configurationManager.GetCVar(cvar)}");

            return false;
        }
    }
}
