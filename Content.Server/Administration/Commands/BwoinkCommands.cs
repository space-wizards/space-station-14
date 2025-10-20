using Content.Server.Administration.Managers.Bwoink;
using Content.Shared.Administration;
using Content.Shared.Administration.Managers.Bwoink;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server.Administration.Commands;

[ToolshedCommand]
[AdminCommand(AdminFlags.Debug)]
public sealed class BwoinkCommand : ToolshedCommand
{
    [Dependency] private readonly ServerBwoinkManager _bwoinkManager = default!;

    [CommandImplementation("message")]
    public void Message([PipedArgument] IEnumerable<ICommonSession> input, [CommandArgument] ProtoId<BwoinkChannelPrototype> channel, [CommandArgument] string message)
    {
        foreach (var session in input)
        {
            _bwoinkManager.SendMessageInChannel(channel, session.UserId, message, MessageFlags.Manager);
        }
    }
}
