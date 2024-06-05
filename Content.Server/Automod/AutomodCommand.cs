using Content.Shared.Automod;
using Content.Shared.Database;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Server.Automod;

[ToolshedCommand]
public sealed class AutomodCommand : ToolshedCommand
{
    [Dependency]
    private IAutomodManager _automodMan = default!;

    [CommandImplementation("reload")]
    public void ReloadAutomodFilters()
    {
        _automodMan.ReloadAutomodFilters();
    }

    [CommandImplementation("add")]
    public void AddAutomodFilter(
            [CommandInvocationContext] IInvocationContext ctx,
            [CommandArgument] string pattern,
            [CommandArgument] AutomodFilterType filterType,
            [CommandArgument] Prototype<AutomodActionGroupPrototype> actionGroup,
            [CommandArgument] AutomodTarget target,
            [CommandArgument] string name
        )
    {
        _automodMan.CreateFilter(new AutomodFilterDef(pattern, filterType, actionGroup.Id, target, name));
    }

    [CommandImplementation("edit")]
    public void EditAutomodFilter(
            [CommandInvocationContext] IInvocationContext ctx,
            [CommandArgument] int id,
            [CommandArgument] string pattern,
            [CommandArgument] AutomodFilterType filterType,
            [CommandArgument] Prototype<AutomodActionGroupPrototype> actionGroup,
            [CommandArgument] AutomodTarget target,
            [CommandArgument] string name
        )
    {
        _automodMan.EditFilter(new AutomodFilterDef(id, pattern, filterType, actionGroup.Id, target, name));
    }
}
