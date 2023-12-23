using Content.Client.ContextMenu.UI;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Client.Commands
{
    public sealed class GroupingEntityMenuCommand : IConsoleCommand
    {
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        // ReSharper disable once StringLiteralTypo
        public string Command => "entitymenug";
        public string Description => Loc.GetString("grouping-entity-menu-command-description");
        public string Help => Loc.GetString("grouping-entity-menu-command-help", ("command", Command), ("groupingTypesCount", EntityMenuUIController.GroupingTypesCount));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine(Help);
                return;
            }

            if (!int.TryParse(args[0], out var id))
            {
                shell.WriteError(Loc.GetString("grouping-entity-menu-command-error", ("arg", args[0])));
                return;
            }

            if (id < 0 || id > EntityMenuUIController.GroupingTypesCount - 1)
            {
                shell.WriteError(Loc.GetString("grouping-entity-menu-command-error", ("arg", args[0])));
                return;
            }

            var cvar = CCVars.EntityMenuGroupingType;

            _configurationManager.SetCVar(cvar, id);
            shell.WriteLine(Loc.GetString("grouping-entity-menu-command-notify", ("cvar", _configurationManager.GetCVar(cvar))));
        }
    }
}
