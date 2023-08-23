using System.Linq;
using Content.Server.Administration;
using Content.Server.Polymorph.Systems;
using Content.Shared.Administration;
using Content.Shared.Polymorph;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Server.Polymorph.Toolshed;

/// <summary>
///     Polymorphs the given entity(s) into the target morph.
/// </summary>
[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed class PolymorphCommand : ToolshedCommand
{
    private PolymorphSystem? _system;

    [CommandImplementation]
    public EntityUid? Polymorph(
            [PipedArgument] EntityUid input,
            [CommandArgument] Prototype<PolymorphPrototype> prototype
        )
    {
        _system ??= GetSys<PolymorphSystem>();

        return _system.PolymorphEntity(input, prototype.Value);
    }

    [CommandImplementation]
    public IEnumerable<EntityUid> Polymorph(
            [PipedArgument] IEnumerable<EntityUid> input,
            [CommandArgument] Prototype<PolymorphPrototype> prototype
        )
        => input.Select(x => Polymorph(x, prototype)).Where(x => x is not null).Select(x => (EntityUid)x!);
}
