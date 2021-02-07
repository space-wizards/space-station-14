using Content.Client.GameObjects.EntitySystems;
using Content.Shared;
using Robust.Shared.Console;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;

namespace Content.Client.Commands
{
    public class GroupingContextMenuCommand : IConsoleCommand
    {
        public string Command => "contextmenug";

        public string Description => "???.";

        public string Help => ($"Usage: contextmenug <0:{VerbSystem.GroupingTypes-1}>");
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine(Help);
                return;
            }

            if (!int.TryParse(args[0], out var id))
            {
                shell.WriteLine($"{args[0]} is not a valid integer.");
                return;
            }

            if (id < 0 ||id > VerbSystem.GroupingTypes - 1)
            {
                shell.WriteLine($"{args[0]} is not a valid integer.");
                return;
            }

            var configurationManager = IoCManager.Resolve<IConfigurationManager>();
            var cvar = CCVars.ContextMenuGroupingType;

            configurationManager.SetCVar(cvar, id);
            shell.WriteLine($"Context Menu Grouping set to type: {configurationManager.GetCVar(cvar)}");
        }
    }
}
