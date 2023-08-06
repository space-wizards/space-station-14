using Content.Server.Administration.Systems;
using Content.Server.Administration.Toolshed.Attributes;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Shared.Toolshed;

namespace Content.Server.Administration.Toolshed;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class RejuvenateCommand : ToolshedCommand, ICommandAsVerb
{
    public VerbCategory Category => VerbCategory.Debug;
    public LogImpact Impact => LogImpact.Medium;

    private RejuvenateSystem? _rejuvenate;

    [CommandImplementation]
    public IEnumerable<EntityUid> Rejuvenate([PipedArgument] IEnumerable<EntityUid> input)
    {
        _rejuvenate ??= GetSys<RejuvenateSystem>();

        foreach (var i in input)
        {
            _rejuvenate.PerformRejuvenate(i);
            yield return i;
        }
    }




}
