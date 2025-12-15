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
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;

    [CommandImplementation("message")]
    public void Message(IInvocationContext ctx, [PipedArgument] IEnumerable<ICommonSession> input, [CommandArgument] ProtoId<BwoinkChannelPrototype> channel, [CommandArgument] string message)
    {
        if (ctx.Session is not null && !_bwoinkManager.CanManageChannel(channel, ctx.Session))
        {
            ctx.WriteLine(_localizationManager.GetString("command-description-bwoink-1984", [("proto", channel)]));
            return;
        }

        foreach (var session in input)
        {
            if (ctx.Session is not null)
            {
                _bwoinkManager.SendMessageInChannel(channel, session.UserId, message, MessageFlags.Manager, ctx.Session.UserId);
            }
            else
            {
                _bwoinkManager.SendMessageInChannel(channel, session.UserId, message, MessageFlags.Manager);
            }
        }
    }

    [CommandImplementation("addchannel")]
    public void AddChannel([PipedArgument] IEnumerable<ICommonSession> input,
        [CommandArgument] ProtoId<BwoinkChannelPrototype> channel)
    {
        foreach (var session in input)
        {
            _bwoinkManager.SetAllowList(channel, session.UserId, true);
        }
    }

    [CommandImplementation("rmchannel")]
    public void RemoveChannel([PipedArgument] IEnumerable<ICommonSession> input,
        [CommandArgument] ProtoId<BwoinkChannelPrototype> channel)
    {
        foreach (var session in input)
        {
            _bwoinkManager.SetAllowList(channel, session.UserId, false);
        }
    }

    [CommandImplementation("sync")]
    public void SyncPlayer([PipedArgument] IEnumerable<ICommonSession> input)
    {
        foreach (var session in input)
        {
            _bwoinkManager.SynchronizeMessages(session);
            _bwoinkManager.SyncChannels(session);
        }
    }
}
