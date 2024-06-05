using Content.Shared.Censor;
using Content.Shared.Database;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Server.Censor;

[ToolshedCommand]
public sealed class CensorCommand : ToolshedCommand
{
    [Dependency]
    private ICensorManager _censorMan = default!;

    [CommandImplementation("add")]
    public void AddCensor(
            [CommandInvocationContext] IInvocationContext ctx,
            [CommandArgument] string filter,
            [CommandArgument] ValueRef<CensorFilterType> filterType,
            [CommandArgument] ValueRef<string, Prototype<CensorActionGroupPrototype>> actionGroup,
            [CommandArgument] ValueRef<CensorTarget> target,
            [CommandArgument] string name
        )
    {
        var parsedActionGroup = actionGroup.Evaluate(ctx);

        if (parsedActionGroup == null)
        {
            Console.WriteLine($"ActionGroup ({actionGroup}) was null.");
            return;
        }

        _censorMan.CreateCensor(new CensorFilterDef(filter,
            filterType.Evaluate(ctx),
            parsedActionGroup,
            target.Evaluate(ctx),
            name));
    }

    [CommandImplementation("addregex")]
    public void GenerateCensor([CommandInvocationContext] IInvocationContext ctx,
        [CommandArgument] string filter,
        [CommandArgument] string name)
    {
        _censorMan.CreateCensor(new CensorFilterDef(filter,
            CensorFilterType.Regex,
            "warning",
            CensorTarget.OOC | CensorTarget.IC,
            $"{name} censor"));
    }

    [CommandImplementation("reload")]
    public void ReloadCensors()
    {
        _censorMan.ReloadCensors();
    }
}
