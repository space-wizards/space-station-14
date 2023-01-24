using Content.Client.ContextMenu.UI;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Content.Client.Commands
{
    public sealed class GroupingEntityMenuCommand : IConsoleCommand
    {
        public string Command => "entitymenug";

        public string Description => "Sets the entity menu grouping type.";

        public string Help => $"Usage: entitymenug <0:{EntityMenuUIController.GroupingTypesCount}>";
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

            if (id < 0 ||id > EntityMenuUIController.GroupingTypesCount - 1)
            {
                shell.WriteLine($"{args[0]} is not a valid integer.");
                return;
            }

            var configurationManager = IoCManager.Resolve<IConfigurationManager>();
            var cvar = CCVars.EntityMenuGroupingType;

            configurationManager.SetCVar(cvar, id);
            shell.WriteLine($"Context Menu Grouping set to type: {configurationManager.GetCVar(cvar)}");
        }
    }
}
