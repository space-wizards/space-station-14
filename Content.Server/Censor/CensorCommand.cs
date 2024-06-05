using Content.Shared.Censor;
using Content.Shared.Database;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Server.Censor;

[ToolshedCommand]
public sealed class CensorCommand : ToolshedCommand
{
    [Dependency]
    private ICensorManager _censorMan = default!;

    [CommandImplementation("reload")]
    public void ReloadCensors()
    {
        _censorMan.ReloadCensors();
    }

    [CommandImplementation("add")]
    public void AddCensor(
            [CommandInvocationContext] IInvocationContext ctx,
            [CommandArgument] string filter,
            [CommandArgument] CensorFilterType filterType,
            [CommandArgument] Prototype<CensorActionGroupPrototype> actionGroup,
            [CommandArgument] CensorTarget target,
            [CommandArgument] string name
        )
    {
        _censorMan.CreateCensor(new CensorFilterDef(filter,
            filterType,
            actionGroup.Id,
            target,
            name));
    }
}
