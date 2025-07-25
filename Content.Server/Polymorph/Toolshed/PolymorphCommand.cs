using System.Linq;
using Content.Server.Administration;
using Content.Server.Polymorph.Systems;
using Content.Shared.Administration;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server.Polymorph.Toolshed;

/// <summary>
///     Polymorphs the given entity(s) into the target morph.
/// </summary>
[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed class PolymorphCommand : ToolshedCommand
{
    private PolymorphSystem? _system;
    [Dependency] private IPrototypeManager _proto = default!;

    [CommandImplementation]
    public EntityUid? Polymorph(
            [PipedArgument] EntityUid input,
            ProtoId<PolymorphPrototype> protoId
        )
    {
        _system ??= GetSys<PolymorphSystem>();

        if (!_proto.TryIndex(protoId, out var prototype))
            return null;

        return _system.PolymorphEntity(input, prototype.Configuration);
    }

    [CommandImplementation]
    public IEnumerable<EntityUid> Polymorph(
            [PipedArgument] IEnumerable<EntityUid> input,
            ProtoId<PolymorphPrototype> protoId
        )
        => input.Select(x => Polymorph(x, protoId)).Where(x => x is not null).Select(x => (EntityUid)x!);
}
