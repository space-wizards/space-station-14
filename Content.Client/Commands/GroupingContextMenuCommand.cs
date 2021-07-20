using Content.Shared;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using ContextMenuView = Content.Client.ContextMenu.UI.ContextMenuView;

namespace Content.Client.Commands
{
    public class GroupingContextMenuCommand : IConsoleCommand
    {
        public string Command => "contextmenug";

        public string Description => "Sets the contextmenu-groupingtype.";

        public string Help => ($"Usage: contextmenug <0:{ContextMenuView.GroupingTypesCount}>");
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

            if (id < 0 ||id > ContextMenuView.GroupingTypesCount - 1)
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
