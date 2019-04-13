using SS14.Shared.Console;
using SS14.Shared.Interfaces.Network;

namespace Content.Server.Interfaces.Chat
{
    public interface IChatCommand : ICommand
    {
        void Execute(IChatManager manager, INetChannel client, params string[] args);
    }
}
