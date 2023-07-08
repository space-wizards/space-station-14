using System.Linq;
using Robust.Server.Player;
using Robust.Shared.Players;

namespace Content.Server.NewCon.Commands.Players;

[ConsoleCommand]
public sealed class GetPlayerEntityCommand : ConsoleCommand
{
    [CommandImplementation]
    public IEnumerable<EntityUid> GetPlayerEntity([PipedArgument] IEnumerable<IPlayerSession> sessions)
    {
        return sessions.Select(x => x.AttachedEntity).Where(x => x is not null).Cast<EntityUid>();
    }
}
