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
            var _configurationManager = IoCManager.Resolve<IConfigurationManager>();
            var old = _configurationManager.GetCVar<bool>("outline.enabled");
            _configurationManager.SetCVar("outline.enabled", !old);
            console.AddLine($"Draw outlines set to: {_configurationManager.GetCVar<bool>("outline.enabled")}");
            return false;
        }
    }
}
