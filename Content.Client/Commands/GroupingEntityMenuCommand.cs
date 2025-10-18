using Content.Client.ContextMenu.UI;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Client.Commands;

public sealed class GroupingEntityMenuCommand : LocalizedCommands
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    public override string Command => "entitymenug";

    public override string Help => Loc.GetString($"cmd-{Command}-help", ("command", Command), ("groupingTypesCount", EntityMenuUIController.GroupingTypesCount));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Help);
            return;
        }

        if (!int.TryParse(args[0], out var id))
        {
            shell.WriteError(Loc.GetString("cmd-entitymenug-error", ("arg", args[0])));
            return;
        }

        if (id < 0 || id > EntityMenuUIController.GroupingTypesCount - 1)
        {
            shell.WriteError(Loc.GetString("cmd-entitymenug-error", ("arg", args[0])));
            return;
        }

        var cvar = CCVars.EntityMenuGroupingType;

        _configurationManager.SetCVar(cvar, id);
        shell.WriteLine(Loc.GetString("cmd-entitymenug-notify", ("cvar", _configurationManager.GetCVar(cvar))));
    }
}
