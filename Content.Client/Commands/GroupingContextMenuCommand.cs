using Content.Client.GameObjects.EntitySystems;
using Content.Shared;
using Robust.Client.Interfaces.Console;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;

namespace Content.Client.Commands
{
    public class GroupingContextMenuCommand : IConsoleCommand
    {
        public string Command => "contextmenug";

        public string Description => "???.";

        public string Help => ($"Usage: contextmenug <0:{VerbSystem.GroupingTypes-1}>");

        public bool Execute(IDebugConsole console, params string[] args)
        {
            if (args.Length != 1)
            {
                console.AddLine(Help);
                return false;
            }

            if (!int.TryParse(args[0], out var id))
            {
                console.AddLine($"{args[0]} is not a valid integer.");
                return false;
            }

            if (id < 0 ||id > VerbSystem.GroupingTypes - 1)
            {
                console.AddLine($"{args[0]} is not a valid integer.");
                return false;
            }

            var configurationManager = IoCManager.Resolve<IConfigurationManager>();
            var cvar = CCVars.ContextMenuGroupingType;

            configurationManager.SetCVar(cvar, id);
            console.AddLine($"Context Menu Grouping set to type: {configurationManager.GetCVar(cvar)}");

            return false;
        }
    }
}
