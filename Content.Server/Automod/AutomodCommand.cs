using System.Diagnostics;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Automod;
using Content.Shared.Database;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Content.Server.Automod;

[ToolshedCommand, AdminCommand(AdminFlags.Automod)]
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
            [CommandArgument] ProtoId<AutomodActionGroupPrototype> actionGroup,
            [CommandArgument] AutomodTarget target,
            [CommandArgument] string name
        )
    {
        _automodMan.CreateFilter(new AutomodFilterDef(pattern, filterType, actionGroup.Id, target, name));
    }

    [CommandImplementation("get")]
    public async void GetAutomodFilter([CommandInvocationContext] IInvocationContext ctx, [CommandArgument] int id)
    {
        var filter = await _automodMan.GetFilter(id);
        if (filter is null)
        {
            ctx.WriteError(new NoFilterFoundError(id));
            return;
        }

        ctx.WriteLine($"Id: {filter.Id}, pattern: {filter.Pattern}, filterType: {filter.FilterType
            }, actionGroup: {filter.ActionGroup}, targets: {filter.TargetFlags}, displayName: {filter.DisplayName}");
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

    [CommandImplementation("remove")]
    public async void RemoveAutomodFilter([CommandInvocationContext] IInvocationContext ctx,
        [CommandArgument] int id)
    {
        if (await _automodMan.RemoveFilter(id))
            ctx.WriteLine("Filters have been removed.");
        else
            ctx.WriteLine("Unable to find filter.");
    }
}

public record struct NoFilterFoundError(int Id) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkupOrThrow($"No filter with the id {Id} could be found.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
