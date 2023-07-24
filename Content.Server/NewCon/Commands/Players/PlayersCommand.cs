using System.Linq;
using Content.Server.NewCon.Errors;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server.NewCon.Commands.Players;

[ConsoleCommand]
public sealed class PlayerCommand : ConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    [CommandImplementation("list")]
    public IEnumerable<IPlayerSession> Players()
        => _playerManager.ServerSessions;

    [CommandImplementation("self")]
    public IPlayerSession Self([CommandInvocationContext] IInvocationContext ctx)
    {
        if (ctx.Session is null)
        {
            ctx.ReportError(new NotForServerConsoleError());
        }

        return (IPlayerSession)ctx.Session!;
    }

    [CommandImplementation("imm")]
    public IPlayerSession Immediate(
        [CommandInvocationContext] IInvocationContext ctx,
        [CommandArgument] string username
        )
    {
        _playerManager.TryGetSessionByUsername(username, out var session);

        if (session is null)
        {
            if (Guid.TryParse(username, out var guid))
            {
                _playerManager.TryGetSessionById(new NetUserId(guid), out session);

                if (session is null)
                {
                    ctx.WriteError(new NoSuchPlayerError(username));
                }
            }
        }

        return session!;
    }

    [CommandImplementation("entity")]
    public IEnumerable<EntityUid> GetPlayerEntity([PipedArgument] IEnumerable<IPlayerSession> sessions)
    {
        return sessions.Select(x => x.AttachedEntity).Where(x => x is not null).Cast<EntityUid>();
    }
}

public record struct NoSuchPlayerError(string Username) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup($"No player with the username/GUID {Username} could be found.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
}
