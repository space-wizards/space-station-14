using System.Linq;
using Content.Server.Administration;
using Content.Server.Polymorph.Systems;
using Content.Shared.Administration;
using Content.Shared.Polymorph;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Server.Polymorph.Toolshed;

/// <summary>
///     Undoes a polymorph, reverting the target to it's original form.
/// </summary>
[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed class UnpolymorphCommand : ToolshedCommand
{
    private PolymorphSystem? _system;

    [CommandImplementation]
    public EntityUid? Unpolymorph([PipedArgument] EntityUid input)
    {
        _system ??= GetSys<PolymorphSystem>();

        return _system.Revert(input);
    }

    [CommandImplementation]
    public IEnumerable<EntityUid> Unpolymorph([PipedArgument] IEnumerable<EntityUid> input)
        => input.Select(Unpolymorph).Where(x => x is not null).Select(x => (EntityUid)x!);
}
